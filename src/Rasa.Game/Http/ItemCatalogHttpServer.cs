using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Rasa.Http
{
    using Config;
    using Data;
    using Managers;
    using Structures;

    /// <summary>
    /// Small read-only HTTP server for the item catalog. It intentionally does
    /// not expose any mutation route: granting items remains the job of the
    /// normal authenticated .giveitem GM command.
    /// </summary>
    public sealed class ItemCatalogHttpServer
    {
        private static readonly Lazy<ItemCatalogHttpServer> LazyInstance =
            new(() => new ItemCatalogHttpServer());

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly object _lock = new();
        private TcpListener _listener;
        private CancellationTokenSource _cancellation;
        private Task _acceptTask;
        private string _bindAddress;
        private int _port;
        private int _backlog;

        public static ItemCatalogHttpServer Instance => LazyInstance.Value;

        private ItemCatalogHttpServer()
        {
        }

        public void Apply(ItemCatalogConfig config)
        {
            config ??= new ItemCatalogConfig();

            lock (_lock)
            {
                if (!config.Enabled)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Initialize, "Item catalog HTTP API is disabled (ItemCatalogConfig.Enabled)." );
                    return;
                }

                var bindAddress = string.IsNullOrWhiteSpace(config.BindAddress)
                    ? "0.0.0.0"
                    : config.BindAddress.Trim();
                var port = config.Port;
                var backlog = config.Backlog > 0 ? config.Backlog : 64;

                if (port <= 0 || port > 65535)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Error, $"Invalid ItemCatalogConfig.Port: {port}. Item catalog HTTP API is off.");
                    return;
                }

                if (_listener != null
                    && string.Equals(_bindAddress, bindAddress, StringComparison.OrdinalIgnoreCase)
                    && _port == port
                    && _backlog == backlog)
                    return;

                StopLocked();

                if (!TryResolveBindAddress(bindAddress, out var address))
                {
                    Logger.WriteLog(LogType.Error, $"Invalid ItemCatalogConfig.BindAddress: {bindAddress}. Item catalog HTTP API is off.");
                    return;
                }

                try
                {
                    _listener = new TcpListener(address, port);
                    _listener.Start(backlog);
                    _bindAddress = bindAddress;
                    _port = port;
                    _backlog = backlog;
                    _cancellation = new CancellationTokenSource();
                    _acceptTask = Task.Run(() => AcceptLoop(_listener, _cancellation.Token));

                    Logger.WriteLog(LogType.Network,
                        $"*** Item catalog HTTP API listening on {bindAddress}:{port} (read-only)");
                }
                catch (Exception e)
                {
                    StopLocked();
                    Logger.WriteLog(LogType.Error, $"Unable to start item catalog HTTP API on {bindAddress}:{port}: {e}");
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
                StopLocked();
        }

        private void StopLocked()
        {
            try
            {
                _cancellation?.Cancel();
            }
            catch
            {
            }

            try
            {
                _listener?.Stop();
            }
            catch
            {
            }

            _listener = null;
            _cancellation?.Dispose();
            _cancellation = null;
            _acceptTask = null;
            _bindAddress = null;
            _port = 0;
            _backlog = 0;
        }

        private static bool TryResolveBindAddress(string value, out IPAddress address)
        {
            if (value == "*" || value == "+" || value == "0.0.0.0")
            {
                address = IPAddress.Any;
                return true;
            }

            if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                address = IPAddress.Loopback;
                return true;
            }

            return IPAddress.TryParse(value, out address);
        }

        private static void AcceptLoop(TcpListener listener, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (SocketException)
                {
                    if (!cancellation.IsCancellationRequested)
                        Logger.WriteLog(LogType.Error, "Item catalog HTTP accept failed.");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (!cancellation.IsCancellationRequested)
                        Logger.WriteLog(LogType.Error, $"Item catalog HTTP accept failed: {e}");
                }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            using (client)
            {
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                try
                {
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true);

                    var requestLine = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(requestLine))
                        return;

                    string header;
                    do
                    {
                        header = reader.ReadLine();
                    }
                    while (!string.IsNullOrEmpty(header));

                    var requestParts = requestLine.Split(' ');
                    if (requestParts.Length < 2)
                    {
                        WriteJson(stream, 400, new ErrorResponse("Malformed request."));
                        return;
                    }

                    if (!string.Equals(requestParts[0], "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteJson(stream, 405, new ErrorResponse("This API is read-only; only GET is supported."), "Allow: GET\r\n");
                        return;
                    }

                    Route(stream, requestParts[1]);
                }
                catch (IOException)
                {
                    // The caller went away or timed out. Nothing to do.
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Item catalog HTTP request failed: {e}");
                }
            }
        }

        private static void Route(NetworkStream stream, string rawTarget)
        {
            var queryIndex = rawTarget.IndexOf('?');
            var rawPath = queryIndex >= 0 ? rawTarget.Substring(0, queryIndex) : rawTarget;
            var rawQuery = queryIndex >= 0 ? rawTarget.Substring(queryIndex + 1) : string.Empty;
            var path = Uri.UnescapeDataString(rawPath).TrimEnd('/');

            if (path.Length == 0)
                path = "/";

            if (string.Equals(path, "/api/items/categories", StringComparison.OrdinalIgnoreCase))
            {
                WriteJson(stream, 200, Enum.GetNames(typeof(InventoryCategory)));
                return;
            }

            if (string.Equals(path, "/api/items", StringComparison.OrdinalIgnoreCase))
            {
                HandleItemSearch(stream, ParseQuery(rawQuery));
                return;
            }

            const string detailPrefix = "/api/items/";
            if (path.StartsWith(detailPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var idText = path.Substring(detailPrefix.Length);
                if (!uint.TryParse(idText, out var templateId))
                {
                    WriteJson(stream, 400, new ErrorResponse("Item template id must be an unsigned integer."));
                    return;
                }

                HandleItemDetails(stream, templateId);
                return;
            }

            WriteJson(stream, 404, new ErrorResponse("Not found."));
        }

        private static void HandleItemSearch(NetworkStream stream, Dictionary<string, string> query)
        {
            if (!query.TryGetValue("category", out var categoryText) || string.IsNullOrWhiteSpace(categoryText))
            {
                WriteJson(stream, 400, new ErrorResponse("category is required."));
                return;
            }

            if (!Enum.TryParse(categoryText, true, out InventoryCategory category)
                || !Enum.IsDefined(typeof(InventoryCategory), category))
            {
                WriteJson(stream, 400, new ErrorResponse(
                    $"Unknown category '{categoryText}'. Expected one of: {string.Join(", ", Enum.GetNames(typeof(InventoryCategory)))}."));
                return;
            }

            query.TryGetValue("search", out var search);
            search = search?.Trim() ?? string.Empty;

            var items = new List<ItemSummary>();

            foreach (var pair in ItemManager.Instance.ItemTemplateItemClass)
            {
                var templateId = pair.Key;
                var classId = pair.Value;

                if (!EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var entityClass))
                    continue;

                if (!entityClass.ItemTemplates.TryGetValue(templateId, out var template))
                    continue;

                if (template.InventoryCategory != category)
                    continue;

                var name = entityClass.ClassName ?? string.Empty;
                if (search.Length > 0 && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                items.Add(ToSummary(template, entityClass));
            }

            items.Sort((left, right) =>
            {
                var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : left.TemplateId.CompareTo(right.TemplateId);
            });

            WriteJson(stream, 200, new ItemSearchResponse(category.ToString(), search, items));
        }

        private static void HandleItemDetails(NetworkStream stream, uint templateId)
        {
            if (!ItemManager.Instance.ItemTemplateItemClass.TryGetValue(templateId, out var classId)
                || !EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var entityClass)
                || !entityClass.ItemTemplates.TryGetValue(templateId, out var template))
            {
                WriteJson(stream, 404, new ErrorResponse($"Unknown item template id {templateId}."));
                return;
            }

            WriteJson(stream, 200, ToDetails(template, entityClass));
        }

        private static ItemSummary ToSummary(ItemTemplate template, EntityClass entityClass)
        {
            return new ItemSummary(
                template.ItemTemplateId,
                entityClass.ClassId,
                entityClass.ClassName ?? string.Empty,
                template.InventoryCategory.ToString(),
                entityClass.ItemClassInfo?.StackSize ?? 0,
                template.QualityId);
        }

        private static ItemDetails ToDetails(ItemTemplate template, EntityClass entityClass)
        {
            var itemClass = entityClass.ItemClassInfo;
            var equipable = entityClass.EquipableClassInfo;
            var weaponClass = entityClass.WeaponClassInfo;
            var weapon = template.WeaponInfo;

            var requirements = template.ItemInfo?.Requirements == null
                ? new Dictionary<string, int>()
                : template.ItemInfo.Requirements.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value);

            return new ItemDetails
            {
                TemplateId = template.ItemTemplateId,
                ItemClassId = entityClass.ClassId,
                Name = entityClass.ClassName ?? string.Empty,
                Category = template.InventoryCategory.ToString(),
                StackSize = itemClass?.StackSize ?? 0,
                QualityId = template.QualityId,
                InventoryIconStringId = itemClass?.InventoryIconStringId ?? 0,
                LootValue = itemClass?.LootValue ?? 0,
                MaxHitPoints = itemClass?.MaxHitPoints ?? 0,
                IsConsumable = itemClass?.IsConsumableFlag != 0,
                BuyPrice = template.BuyPrice,
                SellPrice = template.SellPrice,
                ArmorValue = template.ArmorValue,
                BoundToCharacter = template.BoundToCharacter,
                BindOnEquip = template.HasBoEFlag,
                Sellable = template.HasSellableFlag,
                CharacterUnique = template.HasCharacterUniqueFlag,
                AccountUnique = template.HasAccountUniqueFlag,
                NotTradable = template.NotTradable,
                NotPlaceableInLockbox = template.NotPlaceableInLockbox,
                EquipmentSlot = equipable?.EquipmentSlotId.ToString(),
                RequiredSkillId = template.EquipableInfo?.SkillId,
                RequiredSkillLevel = template.EquipableInfo?.SkillLevel,
                Requirements = requirements,
                Weapon = weaponClass == null && weapon == null
                    ? null
                    : new WeaponDetails
                    {
                        MinDamage = weaponClass?.MinDamage,
                        MaxDamage = weaponClass?.MaxDamage,
                        DamageType = weaponClass?.DamageType,
                        AmmoClassId = weaponClass == null ? null : (uint?)weaponClass.AmmoClassId,
                        ClipSize = weaponClass?.ClipSize,
                        AmmoPerShot = weapon?.AmmoPerShot,
                        Range = weapon?.Range,
                        Windup = weapon?.Windup,
                        Recovery = weapon?.Recovery,
                        Refire = weapon?.Refire,
                        ReloadTime = weapon?.ReloadTime
                    }
            };
        }

        private static Dictionary<string, string> ParseQuery(string rawQuery)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(rawQuery))
                return result;

            foreach (var part in rawQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var equals = part.IndexOf('=');
                var rawKey = equals >= 0 ? part.Substring(0, equals) : part;
                var rawValue = equals >= 0 ? part.Substring(equals + 1) : string.Empty;
                var key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
                var value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
                result[key] = value;
            }

            return result;
        }

        private static void WriteJson(NetworkStream stream, int statusCode, object value, string extraHeaders = "")
        {
            var body = JsonSerializer.Serialize(value, JsonOptions);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var reason = statusCode switch
            {
                200 => "OK",
                400 => "Bad Request",
                404 => "Not Found",
                405 => "Method Not Allowed",
                _ => "Error"
            };

            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {statusCode} {reason}\r\n"
                + "Content-Type: application/json; charset=utf-8\r\n"
                + $"Content-Length: {bodyBytes.Length}\r\n"
                + "Connection: close\r\n"
                + "Cache-Control: no-store\r\n"
                + extraHeaders
                + "\r\n");

            stream.Write(headers, 0, headers.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
            stream.Flush();
        }

        private sealed record ErrorResponse(string Error);
        private sealed record ItemSearchResponse(string Category, string Search, List<ItemSummary> Items);
        private sealed record ItemSummary(uint TemplateId, uint ItemClassId, string Name, string Category, uint StackSize, int QualityId);

        private sealed class ItemDetails
        {
            public uint TemplateId { get; set; }
            public uint ItemClassId { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public uint StackSize { get; set; }
            public int QualityId { get; set; }
            public uint InventoryIconStringId { get; set; }
            public uint LootValue { get; set; }
            public int MaxHitPoints { get; set; }
            public bool IsConsumable { get; set; }
            public int BuyPrice { get; set; }
            public int SellPrice { get; set; }
            public int ArmorValue { get; set; }
            public bool BoundToCharacter { get; set; }
            public bool BindOnEquip { get; set; }
            public bool Sellable { get; set; }
            public bool CharacterUnique { get; set; }
            public bool AccountUnique { get; set; }
            public bool NotTradable { get; set; }
            public bool NotPlaceableInLockbox { get; set; }
            public string EquipmentSlot { get; set; }
            public int? RequiredSkillId { get; set; }
            public int? RequiredSkillLevel { get; set; }
            public Dictionary<string, int> Requirements { get; set; }
            public WeaponDetails Weapon { get; set; }
        }

        private sealed class WeaponDetails
        {
            public int? MinDamage { get; set; }
            public int? MaxDamage { get; set; }
            public byte? DamageType { get; set; }
            public uint? AmmoClassId { get; set; }
            public uint? ClipSize { get; set; }
            public uint? AmmoPerShot { get; set; }
            public uint? Range { get; set; }
            public uint? Windup { get; set; }
            public uint? Recovery { get; set; }
            public uint? Refire { get; set; }
            public uint? ReloadTime { get; set; }
        }
    }
}
