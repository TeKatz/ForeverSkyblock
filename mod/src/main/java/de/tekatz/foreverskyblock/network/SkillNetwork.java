package de.tekatz.foreverskyblock.network;

import de.tekatz.foreverskyblock.ForeverSkyblock;
import de.tekatz.foreverskyblock.progression.ContractCatalog;
import de.tekatz.foreverskyblock.progression.MetaProgress;
import de.tekatz.foreverskyblock.progression.MilestoneCatalog;
import de.tekatz.foreverskyblock.progression.ProgressionProfile;
import de.tekatz.foreverskyblock.progression.ProgressionService;
import de.tekatz.foreverskyblock.progression.ShopCatalog;
import de.tekatz.foreverskyblock.progression.SkillCatalog;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.function.Consumer;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.neoforge.network.PacketDistributor;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;

public final class SkillNetwork {
    private static Consumer<Snapshot> clientReceiver = snapshot -> { };
    private SkillNetwork() { }
    public static void setClientReceiver(Consumer<Snapshot> receiver) { clientReceiver = receiver; }
    public static void register(RegisterPayloadHandlersEvent event) {
        var registrar = event.registrar("skills-9");
        registrar.playToServer(Request.TYPE, Request.CODEC, (request, context) -> {
            if (!(context.player() instanceof ServerPlayer player)) return;
            var service = ProgressionService.get(player.server);
            if (request.action() == 0) service.open(player);
            else if (request.action() == 1) service.buy(player, request.skillId());
            else if (request.action() == 2) service.reset(player);
            else if (request.action() == 3) service.testPoints(player);
            else if (request.action() == 4) service.buyProduct(player, request.skillId());
            else if (request.action() == 5) service.claimProduct(player, request.skillId());
            else if (request.action() == 6) service.refreshMenu(player);
        });
        registrar.playToServer(ContractRequest.TYPE, ContractRequest.CODEC, (request, context) -> {
            if (context.player() instanceof ServerPlayer player) ProgressionService.get(player.server).submitContract(player, request.contractId(), request.requestId());
        });
        registrar.playToClient(Snapshot.TYPE, Snapshot.CODEC, (snapshot, context) -> clientReceiver.accept(snapshot));
    }
    public static void send(ServerPlayer player, ProgressionProfile profile, boolean local, boolean open, boolean available,
                            String message, WorldState world) {
        if (player.connection == null || !player.connection.getConnection().isConnected()
                || !net.neoforged.neoforge.network.registration.NetworkRegistry.hasChannel(player.connection, Snapshot.TYPE.id())) return;
        var skills = new LinkedHashMap<String, Integer>();
        for (var skill : SkillCatalog.SKILLS) if (profile.rank(skill.id()) > 0) skills.put(skill.id(), profile.rank(skill.id()));
        var milestones = new LinkedHashSet<String>();
        for (var milestone : MilestoneCatalog.MILESTONES) if (profile.milestones().contains(milestone.id())) milestones.add(milestone.id());
        var progress = new LinkedHashMap<String, Integer>();
        for (var milestone : MilestoneCatalog.MILESTONES) {
            var state = profile.meta().progress().get(milestone.id());
            progress.put(milestone.id(), profile.milestones().contains(milestone.id()) ? milestone.target()
                    : state == null ? 0 : state.revision() != 1 ? -1 : Math.min(milestone.target(), state.evidence().size()));
        }
        var contracts = new LinkedHashMap<String, ContractState>();
        for (var contract : ContractCatalog.all()) {
            var state = profile.meta().contracts().getOrDefault(contract.id(), MetaProgress.ContractState.empty());
            boolean pending = profile.meta().pendingContracts().values().stream().anyMatch(value -> value.contractId().equals(contract.id()));
            boolean itemAvailable = !contract.resource() || ProgressionService.contractItemAvailable(contract.itemId());
            boolean unlocked = de.tekatz.foreverskyblock.editor.EditorRuntime.contractEnabled(contract.id()) && profile.meta().shopTier() >= contract.requiredTier() && itemAvailable;
            int current = contract.resource() ? Math.min(contract.target(), ProgressionService.inventoryCount(player, contract.itemId())) : state.progress();
            contracts.put(contract.id(), new ContractState(current, state.completions(), unlocked,
                    contract.resource() && unlocked && !pending && current >= contract.target(), pending));
        }
        var products = new LinkedHashMap<String, ProductState>();
        for (var product : ShopCatalog.all()) {
            boolean productAvailable = de.tekatz.foreverskyblock.editor.EditorRuntime.productUnlocked(product.id(),profile) && (!product.developmentOnly() || ProgressionService.canTestPoints(player)) && ProgressionService.productAvailable(product);
            int stage = 0;
            if (world.available() && world.identity() != null) {
                var delivery = profile.meta().delivery(world.identity(), product.id());
                if (delivery != null) stage = delivery.getValue().stage().ordinal() + 1;
                else if (profile.meta().claimed(world.identity(), product.id())) stage = 6;
            }
            products.put(product.id(), new ProductState(stage, productAvailable || stage > 0 && ProgressionService.productAvailable(product)));
        }
        PacketDistributor.sendToPlayer(player, new Snapshot(profile.points(), profile.perkPoints(), skills, milestones, local, open,
                available, message, ProgressionService.canTestPoints(player), profile.meta().shopTier(), progress,
                profile.meta().accounting(), world.available(), world.name(), world.claims(), contracts, products, statistics(player)));
    }
    private static MenuStatistics statistics(ServerPlayer player) {
        var stats = player.getStats();
        int island = -1;
        try { island = de.tekatz.foreverskyblock.progression.WorldQoLService.state(player.server).islandLevel(); }
        catch (java.io.IOException | RuntimeException e) { com.mojang.logging.LogUtils.getLogger().error("Could not read island level", e); }
        return new MenuStatistics(stats.getValue(net.minecraft.stats.Stats.CUSTOM.get(net.minecraft.stats.Stats.PLAY_TIME)),
                island, stats.getValue(net.minecraft.stats.Stats.CUSTOM.get(net.minecraft.stats.Stats.DEATHS)),
                stats.getValue(net.minecraft.stats.Stats.CUSTOM.get(net.minecraft.stats.Stats.MOB_KILLS)));
    }
    /** Per-player Vanilla world statistics plus the shared world-local milestone count. */
    public record MenuStatistics(long playTicks, int islandLevel, int deaths, int mobKills) {
        public static final MenuStatistics UNKNOWN = new MenuStatistics(-1, -1, -1, -1);
        public MenuStatistics {
            if (playTicks < -1 || playTicks > Integer.MAX_VALUE || islandLevel < -1 || islandLevel > 128
                    || deaths < -1 || mobKills < -1) throw new IllegalArgumentException("Invalid menu statistics");
        }
    }
    public record WorldState(boolean available, String name, Set<String> claims, UUID identity) {
        public WorldState {
            name = name == null ? "" : name;
            claims = Set.copyOf(claims);
            if (available && identity == null) throw new IllegalArgumentException("Missing world identity");
        }
        public WorldState(boolean available, String name, Set<String> claims) { this(available, name, claims, null); }
    }
    public record ContractState(int progress, long completions, boolean available, boolean canSubmit, boolean pending) {
        public ContractState {
            if (progress < 0 || progress > 1_000_000 || completions < 0 || completions > 1_000_000_000L)
                throw new IllegalArgumentException("Invalid contract snapshot");
        }
    }
    /** stage: 0 unpurchased, 1 ready, 2 dispatching, 3 delivered, 4 opening, 5 opened, 6 claimed/no delivery. */
    public record ProductState(int stage, boolean available) {
        public ProductState { if (stage < 0 || stage > 6) throw new IllegalArgumentException("Invalid product state"); }
    }
    public record Request(int action, String skillId) implements CustomPacketPayload {
        public Request {
            if (action < 0 || action > 6 || skillId == null) throw new IllegalArgumentException("Invalid progression action");
            if (action == 1 || action == 4 || action == 5) ProgressionProfile.validateId(skillId);
            else if (!skillId.isEmpty()) throw new IllegalArgumentException("Unexpected progression action payload");
        }
        public static final Type<Request> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "skill_request"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Request> CODEC = new StreamCodec<>() {
            @Override public Request decode(RegistryFriendlyByteBuf buffer) { return new Request(buffer.readUnsignedByte(), buffer.readUtf(128)); }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Request request) { buffer.writeByte(request.action()); buffer.writeUtf(request.skillId(), 128); }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
    public record ContractRequest(String contractId, UUID requestId) implements CustomPacketPayload {
        public ContractRequest { ProgressionProfile.validateId(contractId); if (requestId == null) throw new IllegalArgumentException("Missing contract request id"); }
        public static final Type<ContractRequest> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "contract_request"));
        public static final StreamCodec<RegistryFriendlyByteBuf, ContractRequest> CODEC = new StreamCodec<>() {
            @Override public ContractRequest decode(RegistryFriendlyByteBuf buffer) { return new ContractRequest(buffer.readUtf(128), buffer.readUUID()); }
            @Override public void encode(RegistryFriendlyByteBuf buffer, ContractRequest request) { buffer.writeUtf(request.contractId(), 128); buffer.writeUUID(request.requestId()); }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
    }
    public record Snapshot(int points, int perkPoints, Map<String, Integer> skills, Set<String> milestones, boolean local, boolean open,
                           boolean available, String message, boolean canTestPoints, int shopTier, Map<String, Integer> progress,
                           MetaProgress.Accounting accounting, boolean worldAvailable, String worldName,
                           Set<String> worldClaims, Map<String, ContractState> contracts,
                           Map<String, ProductState> products, MenuStatistics statistics) implements CustomPacketPayload {
        public Snapshot {
            skills = Map.copyOf(skills); milestones = Set.copyOf(milestones); progress = Map.copyOf(progress);
            worldClaims = Set.copyOf(worldClaims); contracts = Map.copyOf(contracts); products = Map.copyOf(products);
            if (shopTier < -1 || shopTier > 4 || progress.size() > 128 || skills.size() > 128 || milestones.size() > 128
                    || worldClaims.size() > 128 || contracts.size() > 128 || products.size() > 128
                    || statistics == null || accounting == null || message == null || message.length() > 128
                    || worldName == null || worldName.length() > 128)
                throw new IllegalArgumentException("Invalid progression snapshot");
            progress.forEach((id, count) -> {
                ProgressionProfile.validateId(id); if (count < -1 || count > 1000) throw new IllegalArgumentException("Invalid challenge progress");
            });
            for (String id : worldClaims) ProgressionProfile.validateId(id);
            contracts.forEach((id, state) -> { ProgressionProfile.validateId(id); if (state == null) throw new IllegalArgumentException("Missing contract state"); });
            products.forEach((id, state) -> { ProgressionProfile.validateId(id); if (state == null) throw new IllegalArgumentException("Missing product state"); });
        }
        public Snapshot(int points, int perkPoints, Map<String, Integer> skills, Set<String> milestones, boolean local, boolean open,
                        boolean available, String message, boolean canTestPoints, int shopTier, Map<String, Integer> progress,
                        MetaProgress.Accounting accounting, boolean worldAvailable, String worldName, Set<String> worldClaims,
                        Map<String, ContractState> contracts, Map<String, ProductState> products) {
            this(points, perkPoints, skills, milestones, local, open, available, message, canTestPoints, shopTier, progress,
                    accounting, worldAvailable, worldName, worldClaims, contracts, products, MenuStatistics.UNKNOWN);
        }
        public Snapshot(int points, int perkPoints, Map<String, Integer> skills, Set<String> milestones, boolean local, boolean open,
                        boolean available, String message, boolean canTestPoints, int shopTier, Map<String, Integer> progress,
                        MetaProgress.Accounting accounting, boolean worldAvailable, String worldName, Set<String> worldClaims,
                        Map<String, ContractState> contracts) {
            this(points, perkPoints, skills, milestones, local, open, available, message, canTestPoints, shopTier, progress,
                    accounting, worldAvailable, worldName, worldClaims, contracts, Map.of());
        }
        public Snapshot(int points, int perkPoints, Map<String, Integer> skills, Set<String> milestones, boolean local,
                        boolean open, boolean available, String message, boolean canTestPoints) {
            this(points, perkPoints, skills, milestones, local, open, available, message, canTestPoints,
                    de.tekatz.foreverskyblock.progression.ChallengeRules.qualifiedTier(milestones), Map.of(),
                    MetaProgress.Accounting.empty(true), false, "", Set.of(), Map.of(), Map.of());
        }
        public static final Type<Snapshot> TYPE = new Type<>(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, "skill_snapshot"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Snapshot> CODEC = new StreamCodec<>() {
            @Override public Snapshot decode(RegistryFriendlyByteBuf buffer) {
                int points = buffer.readVarInt(); if (points < 0 || points > ProgressionProfile.MAX_POINTS) throw new IllegalArgumentException("Invalid skill balance");
                int perks = buffer.readVarInt(); if (perks < 0 || perks > ProgressionProfile.MAX_POINTS) throw new IllegalArgumentException("Invalid perk balance");
                int flags = buffer.readUnsignedByte(); String message = buffer.readUtf(128);
                int size = size(buffer); var skills = new LinkedHashMap<String, Integer>();
                for (int i = 0; i < size; i++) { String id = buffer.readUtf(128); ProgressionProfile.validateId(id); int rank = buffer.readVarInt(); if (rank < 1 || rank > 100) throw new IllegalArgumentException("Invalid skill rank"); skills.put(id, rank); }
                size = size(buffer); var milestones = new LinkedHashSet<String>();
                for (int i = 0; i < size; i++) { String id = buffer.readUtf(128); ProgressionProfile.validateId(id); milestones.add(id); }
                int tier = buffer.readVarInt(); size = size(buffer); var progress = new LinkedHashMap<String, Integer>();
                for (int i = 0; i < size; i++) { String id = buffer.readUtf(128); int count = buffer.readVarInt(); if (progress.put(id, count) != null) throw new IllegalArgumentException("Duplicate progress entry"); }
                var accounting = new MetaProgress.Accounting(buffer.readVarLong(), buffer.readVarLong(), buffer.readVarLong(),
                        buffer.readVarLong(), buffer.readVarLong(), buffer.readVarLong(), buffer.readVarLong(), buffer.readBoolean());
                boolean worldAvailable = buffer.readBoolean(); String worldName = buffer.readUtf(128);
                size = size(buffer); var worldClaims = new LinkedHashSet<String>();
                for (int i = 0; i < size; i++) { String id = buffer.readUtf(128); ProgressionProfile.validateId(id); if (!worldClaims.add(id)) throw new IllegalArgumentException("Duplicate world claim"); }
                size = size(buffer); var contracts = new LinkedHashMap<String, ContractState>();
                for (int i = 0; i < size; i++) {
                    String id = buffer.readUtf(128); ProgressionProfile.validateId(id);
                    var state = new ContractState(buffer.readVarInt(), buffer.readVarLong(), buffer.readBoolean(), buffer.readBoolean(), buffer.readBoolean());
                    if (contracts.put(id, state) != null) throw new IllegalArgumentException("Duplicate contract entry");
                }
                size = size(buffer); var products = new LinkedHashMap<String, ProductState>();
                for (int i = 0; i < size; i++) {
                    String id = buffer.readUtf(128); ProgressionProfile.validateId(id);
                    var state = new ProductState(buffer.readUnsignedByte(), buffer.readBoolean());
                    if (products.put(id, state) != null) throw new IllegalArgumentException("Duplicate product entry");
                }
                return new Snapshot(points, perks, skills, milestones, (flags & 1) != 0, (flags & 2) != 0,
                        (flags & 4) != 0, message, (flags & 8) != 0, tier, progress, accounting, worldAvailable, worldName, worldClaims, contracts, products,
                        new MenuStatistics(buffer.readVarLong(), buffer.readVarInt(), buffer.readVarInt(), buffer.readVarInt()));
            }
            private int size(RegistryFriendlyByteBuf buffer) { int count = buffer.readVarInt(); if (count < 0 || count > 128) throw new IllegalArgumentException("Progression packet too large"); return count; }
            @Override public void encode(RegistryFriendlyByteBuf buffer, Snapshot value) {
                buffer.writeVarInt(value.points()); buffer.writeVarInt(value.perkPoints());
                buffer.writeByte((value.local() ? 1 : 0) | (value.open() ? 2 : 0) | (value.available() ? 4 : 0) | (value.canTestPoints() ? 8 : 0));
                buffer.writeUtf(value.message(), 128);
                buffer.writeVarInt(value.skills().size()); value.skills().forEach((id, rank) -> { buffer.writeUtf(id, 128); buffer.writeVarInt(rank); });
                buffer.writeVarInt(value.milestones().size()); value.milestones().forEach(id -> { buffer.writeUtf(id, 128); });
                buffer.writeVarInt(value.shopTier()); buffer.writeVarInt(value.progress().size()); value.progress().forEach((id, count) -> { buffer.writeUtf(id, 128); buffer.writeVarInt(count); });
                var a = value.accounting(); for (long total : new long[]{a.spEarned(), a.ppEarned(), a.spSpent(), a.ppSpent(), a.spRefunded(), a.ppRefunded(), a.spAdmin()}) buffer.writeVarLong(total);
                buffer.writeBoolean(a.legacyHistoryUnknown()); buffer.writeBoolean(value.worldAvailable()); buffer.writeUtf(value.worldName(), 128);
                buffer.writeVarInt(value.worldClaims().size()); value.worldClaims().forEach(id -> buffer.writeUtf(id, 128));
                buffer.writeVarInt(value.contracts().size()); value.contracts().forEach((id, state) -> {
                    buffer.writeUtf(id, 128); buffer.writeVarInt(state.progress()); buffer.writeVarLong(state.completions());
                    buffer.writeBoolean(state.available()); buffer.writeBoolean(state.canSubmit()); buffer.writeBoolean(state.pending());
                });
                buffer.writeVarInt(value.products().size()); value.products().forEach((id, state) -> {
                    buffer.writeUtf(id, 128); buffer.writeByte(state.stage()); buffer.writeBoolean(state.available());
                });
                buffer.writeVarLong(value.statistics().playTicks()); buffer.writeVarInt(value.statistics().islandLevel());
                buffer.writeVarInt(value.statistics().deaths()); buffer.writeVarInt(value.statistics().mobKills());
            }
        };
        @Override public Type<? extends CustomPacketPayload> type() { return TYPE; }
        public int rank(String skill) { return skills.getOrDefault(skill, 0); }
    }
}
