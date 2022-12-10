﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Context.Database;
using Snap.Hutao.Core.Abstraction;
using Snap.Hutao.Core.Database;
using Snap.Hutao.Core.Diagnostics;
using Snap.Hutao.Core.Logging;
using Snap.Hutao.Extension;
using Snap.Hutao.Model.Binding.Gacha;
using Snap.Hutao.Model.Binding.Gacha.Abstraction;
using Snap.Hutao.Model.Entity;
using Snap.Hutao.Model.InterChange.GachaLog;
using Snap.Hutao.Model.Metadata.Abstraction;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.Service.GachaLog.Factory;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.Web.Hoyolab.Hk4e.Event.GachaInfo;
using Snap.Hutao.Web.Response;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Service.GachaLog;

/// <summary>
/// 祈愿记录服务
/// </summary>
[Injection(InjectAs.Scoped, typeof(IGachaLogService))]
internal class GachaLogService : IGachaLogService, ISupportAsyncInitialization
{
    /// <summary>
    /// 祈愿记录查询的类型
    /// </summary>
    private static readonly ImmutableList<GachaConfigType> QueryTypes = new List<GachaConfigType>
    {
        GachaConfigType.NoviceWish,
        GachaConfigType.PermanentWish,
        GachaConfigType.AvatarEventWish,
        GachaConfigType.WeaponEventWish,
    }.ToImmutableList();

    private readonly AppDbContext appDbContext;
    private readonly IEnumerable<IGachaLogUrlProvider> urlProviders;
    private readonly GachaInfoClient gachaInfoClient;
    private readonly IMetadataService metadataService;
    private readonly IGachaStatisticsFactory gachaStatisticsFactory;
    private readonly ILogger<GachaLogService> logger;
    private readonly DbCurrent<GachaArchive, Message.GachaArchiveChangedMessage> dbCurrent;

    private readonly Dictionary<string, ItemBase> itemBaseCache = new();

    private Dictionary<string, Model.Metadata.Avatar.Avatar>? nameAvatarMap;
    private Dictionary<string, Model.Metadata.Weapon.Weapon>? nameWeaponMap;

    private Dictionary<AvatarId, Model.Metadata.Avatar.Avatar>? idAvatarMap;
    private Dictionary<WeaponId, Model.Metadata.Weapon.Weapon>? idWeaponMap;

    private ObservableCollection<GachaArchive>? archiveCollection;

    /// <summary>
    /// 构造一个新的祈愿记录服务
    /// </summary>
    /// <param name="appDbContext">数据库上下文</param>
    /// <param name="urlProviders">Url提供器集合</param>
    /// <param name="gachaInfoClient">祈愿记录客户端</param>
    /// <param name="metadataService">元数据服务</param>
    /// <param name="gachaStatisticsFactory">祈愿统计工厂</param>
    /// <param name="logger">日志器</param>
    /// <param name="messenger">消息器</param>
    public GachaLogService(
        AppDbContext appDbContext,
        IEnumerable<IGachaLogUrlProvider> urlProviders,
        GachaInfoClient gachaInfoClient,
        IMetadataService metadataService,
        IGachaStatisticsFactory gachaStatisticsFactory,
        ILogger<GachaLogService> logger,
        IMessenger messenger)
    {
        this.appDbContext = appDbContext;
        this.urlProviders = urlProviders;
        this.gachaInfoClient = gachaInfoClient;
        this.metadataService = metadataService;
        this.logger = logger;
        this.gachaStatisticsFactory = gachaStatisticsFactory;

        dbCurrent = new(appDbContext.GachaArchives, messenger);
    }

    /// <inheritdoc/>
    public GachaArchive? CurrentArchive
    {
        get => dbCurrent.Current;
        set => dbCurrent.Current = value;
    }

    /// <inheritdoc/>
    public bool IsInitialized { get; set; }

    /// <inheritdoc/>
    public Task<UIGF> ExportToUIGFAsync(GachaArchive archive)
    {
        List<UIGFItem> list = appDbContext.GachaItems
            .Where(i => i.ArchiveId == archive.InnerId)
            .AsEnumerable()
            .Select(i => i.ToUIGFItem(GetNameQualityByItemId(i.ItemId)))
            .ToList();

        UIGF uigf = new()
        {
            Info = UIGFInfo.Create(archive.Uid),
            List = list,
        };

        return Task.FromResult(uigf);
    }

    /// <inheritdoc/>
    public ObservableCollection<GachaArchive> GetArchiveCollection()
    {
        return archiveCollection ??= new(appDbContext.GachaArchives.ToList());
    }

    /// <inheritdoc/>
    public async ValueTask<bool> InitializeAsync()
    {
        if (await metadataService.InitializeAsync().ConfigureAwait(false))
        {
            nameAvatarMap = await metadataService.GetNameToAvatarMapAsync().ConfigureAwait(false);
            nameWeaponMap = await metadataService.GetNameToWeaponMapAsync().ConfigureAwait(false);

            idAvatarMap = await metadataService.GetIdToAvatarMapAsync().ConfigureAwait(false);
            idWeaponMap = await metadataService.GetIdToWeaponMapAsync().ConfigureAwait(false);

            IsInitialized = true;
        }
        else
        {
            IsInitialized = false;
        }

        return IsInitialized;
    }

    /// <inheritdoc/>
    public async Task<GachaStatistics> GetStatisticsAsync(GachaArchive? archive = null)
    {
        archive ??= CurrentArchive;

        // Return statistics
        if (archive != null)
        {
            ValueStopwatch stopwatch = ValueStopwatch.StartNew();
            IQueryable<GachaItem> items = appDbContext.GachaItems
                .Where(i => i.ArchiveId == archive.InnerId);

            GachaStatistics statistics = await gachaStatisticsFactory.CreateAsync(items).ConfigureAwait(false);
            logger.LogInformation(EventIds.GachaStatisticGeneration, "GachaStatistic Generation toke {time} ms.", stopwatch.GetElapsedTime().TotalMilliseconds);
            return statistics;
        }
        else
        {
            throw Must.NeverHappen();
        }
    }

    /// <inheritdoc/>
    public IGachaLogUrlProvider? GetGachaLogUrlProvider(RefreshOption option)
    {
        return option switch
        {
            RefreshOption.WebCache => urlProviders.Single(p => p.Name == nameof(GachaLogUrlWebCacheProvider)),
            RefreshOption.Stoken => urlProviders.Single(p => p.Name == nameof(GachaLogUrlStokenProvider)),
            RefreshOption.ManualInput => urlProviders.Single(p => p.Name == nameof(GachaLogUrlManualInputProvider)),
            _ => null,
        };
    }

    /// <inheritdoc/>
    public async Task ImportFromUIGFAsync(List<UIGFItem> list, string uid)
    {
        await ThreadHelper.SwitchToBackgroundAsync();

        GachaArchive? archive = null;
        SkipOrInitArchive(ref archive, uid);
        Guid archiveId = Must.NotNull(archive!).InnerId;

        long trimId = appDbContext.GachaItems
            .Where(i => i.ArchiveId == archiveId)
            .OrderBy(i => i.Id)
            .FirstOrDefault()?.Id ?? long.MaxValue;

        IEnumerable<GachaItem> toAdd = list
            .OrderByDescending(i => i.Id)
            .Where(i => i.Id < trimId)
            .Select(i => GachaItem.Create(archiveId, i, GetItemId(i)));

        await appDbContext.GachaItems.AddRangeAndSaveAsync(toAdd).ConfigureAwait(false);
        CurrentArchive = archive;
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshGachaLogAsync(string query, RefreshStrategy strategy, IProgress<FetchState> progress, CancellationToken token)
    {
        bool isLazy = strategy switch
        {
            RefreshStrategy.AggressiveMerge => false,
            RefreshStrategy.LazyMerge => true,
            _ => throw Must.NeverHappen(),
        };

        (bool authkeyValid, GachaArchive? result) = await FetchGachaLogsAsync(query, isLazy, progress, token).ConfigureAwait(false);
        CurrentArchive = result ?? CurrentArchive;
        return authkeyValid;
    }

    /// <inheritdoc/>
    public async Task RemoveArchiveAsync(GachaArchive archive)
    {
        // Sync cache
        await ThreadHelper.SwitchToMainThreadAsync();
        archiveCollection!.Remove(archive);

        // Sync database
        await ThreadHelper.SwitchToBackgroundAsync();
        await appDbContext.GachaItems
            .Where(item => item.ArchiveId == archive.InnerId)
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
        await appDbContext.GachaArchives.RemoveAndSaveAsync(archive).ConfigureAwait(false);
    }

    private static Task RandomDelayAsync(CancellationToken token)
    {
        return Task.Delay(TimeSpan.FromSeconds(Random.Shared.NextDouble() + 1), token);
    }

    private async Task<ValueResult<bool, GachaArchive?>> FetchGachaLogsAsync(string query, bool isLazy, IProgress<FetchState> progress, CancellationToken token)
    {
        GachaArchive? archive = null;
        FetchState state = new();

        foreach (GachaConfigType configType in QueryTypes)
        {
            state.ConfigType = configType;
            long? dbEndId = null;
            GachaLogConfigration configration = new(query, configType);
            List<GachaItem> itemsToAdd = new();

            do
            {
                Response<GachaLogPage>? response = await gachaInfoClient.GetGachaLogPageAsync(configration, token).ConfigureAwait(false);

                if (response?.Data is GachaLogPage page)
                {
                    state.Items.Clear();
                    List<GachaLogItem> items = page.List;
                    bool completedCurrentTypeAdding = false;

                    foreach (GachaLogItem item in items)
                    {
                        SkipOrInitArchive(ref archive, item.Uid);
                        dbEndId ??= GetEndId(archive, configType);

                        if ((!isLazy) || item.Id > dbEndId)
                        {
                            itemsToAdd.Add(GachaItem.Create(archive.InnerId, item, GetItemId(item)));
                            state.Items.Add(GetItemBaseByName(item.Name, item.ItemType));
                            configration.EndId = item.Id;
                        }
                        else
                        {
                            completedCurrentTypeAdding = true;
                            break;
                        }
                    }

                    progress.Report(state);

                    if (completedCurrentTypeAdding || items.Count < GachaLogConfigration.Size)
                    {
                        // exit current type fetch loop
                        break;
                    }
                }
                else
                {
                    state.AuthKeyTimeout = true;
                    progress.Report(state);
                    break;
                }

                await RandomDelayAsync(token).ConfigureAwait(false);
            }
            while (true);

            if (state.AuthKeyTimeout)
            {
                break;
            }

            SaveGachaItems(itemsToAdd, isLazy, archive, configration.EndId);
            await RandomDelayAsync(token).ConfigureAwait(false);
        }

        return new(!state.AuthKeyTimeout, archive);
    }

    private void SkipOrInitArchive([NotNull] ref GachaArchive? archive, string uid)
    {
        if (archive == null)
        {
            archive = appDbContext.GachaArchives.SingleOrDefault(a => a.Uid == uid);

            if (archive == null)
            {
                GachaArchive created = GachaArchive.Create(uid);
                appDbContext.GachaArchives.AddAndSave(created);

                archive = appDbContext.GachaArchives.Single(a => a.Uid == uid);
                GachaArchive temp = archive;
                Program.DispatcherQueue!.TryEnqueue(() => archiveCollection!.Add(temp));
            }
        }
    }

    private long GetEndId(GachaArchive? archive, GachaConfigType configType)
    {
        GachaItem? item = null;

        if (archive != null)
        {
            // TODO: replace with MaxBy
            // https://github.com/dotnet/efcore/issues/25566
            // .MaxBy(i => i.Id);
            item = appDbContext.GachaItems
                .Where(i => i.ArchiveId == archive.InnerId)
                .Where(i => i.QueryType == configType)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
        }

        return item?.Id ?? 0L;
    }

    private int GetItemId(GachaLogItem item)
    {
        return item.ItemType switch
        {
            "角色" => nameAvatarMap!.GetValueOrDefault(item.Name)?.Id ?? 0,
            "武器" => nameWeaponMap!.GetValueOrDefault(item.Name)?.Id ?? 0,
            _ => 0,
        };
    }

    private ItemBase GetItemBaseByName(string name, string type)
    {
        if (!itemBaseCache.TryGetValue(name, out ItemBase? result))
        {
            result = type switch
            {
                "角色" => nameAvatarMap![name].ToItemBase(),
                "武器" => nameWeaponMap![name].ToItemBase(),
                _ => throw Must.NeverHappen(),
            };

            itemBaseCache[name] = result;
        }

        return result;
    }

    private INameQuality GetNameQualityByItemId(int id)
    {
        return id.Place() switch
        {
            8 => idAvatarMap![id],
            5 => idWeaponMap![id],
            _ => throw Must.NeverHappen(),
        };
    }

    private void SaveGachaItems(List<GachaItem> itemsToAdd, bool isLazy, GachaArchive? archive, long endId)
    {
        if (itemsToAdd.Count > 0)
        {
            // 全量刷新
            if ((!isLazy) && archive != null)
            {
                appDbContext.GachaItems
                    .Where(i => i.ArchiveId == archive.InnerId)
                    .Where(i => i.Id >= endId)
                    .ExecuteDelete();
            }

            appDbContext.GachaItems.AddRangeAndSave(itemsToAdd);
        }
    }
}