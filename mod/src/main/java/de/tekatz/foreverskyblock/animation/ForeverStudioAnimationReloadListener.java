package de.tekatz.foreverskyblock.animation;

import com.mojang.logging.LogUtils;
import de.tekatz.foreverskyblock.ForeverSkyblock;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.packs.resources.Resource;
import net.minecraft.server.packs.resources.ResourceManager;
import net.minecraft.server.packs.resources.SimplePreparableReloadListener;
import net.minecraft.util.profiling.ProfilerFiller;
import org.slf4j.Logger;

import java.io.IOException;
import java.io.Reader;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.TreeMap;

/** Loads format-v1 Forever Studio animation documents from server data packs. */
public final class ForeverStudioAnimationReloadListener
        extends SimplePreparableReloadListener<ForeverStudioAnimationReloadListener.PreparedAnimations> {

    public static final String DIRECTORY = "forever_studio/animations";
    private static final Logger LOGGER = LogUtils.getLogger();

    @Override
    protected PreparedAnimations prepare(ResourceManager resourceManager, ProfilerFiller profiler) {
        Map<String, ForeverStudioAnimationDocument> loaded = new LinkedHashMap<>();
        int rejected = 0;

        Map<ResourceLocation, Resource> resources = new TreeMap<>(resourceManager.listResources(
                DIRECTORY,
                id -> id.getNamespace().equals(ForeverSkyblock.MOD_ID) && id.getPath().endsWith(".json")));

        for (Map.Entry<ResourceLocation, Resource> entry : resources.entrySet()) {
            ResourceLocation location = entry.getKey();
            try (Reader reader = entry.getValue().openAsReader()) {
                ForeverStudioAnimationDocument document = ForeverStudioAnimationParser.parse(reader);
                String assetKey = ForeverStudioAnimationRegistry.normalize(document.assetId());
                if (assetKey.isEmpty()) throw new IllegalArgumentException("assetId is empty");

                ForeverStudioAnimationDocument previous = loaded.putIfAbsent(assetKey, document);
                if (previous != null) {
                    rejected++;
                    LOGGER.error("Ignoring duplicate Forever Studio animation asset '{}' from {}", document.assetId(), location);
                    continue;
                }

                String fileName = location.getPath().substring(location.getPath().lastIndexOf('/') + 1, location.getPath().length() - 5);
                if (!fileName.equalsIgnoreCase(assetKey)) {
                    LOGGER.warn("Forever Studio animation {} declares assetId '{}' but filename is '{}.json'", location, document.assetId(), fileName);
                }
            } catch (IOException | RuntimeException exception) {
                rejected++;
                LOGGER.error("Could not load Forever Studio animation {}: {}", location, exception.getMessage());
            }
        }

        return new PreparedAnimations(Map.copyOf(loaded), rejected);
    }

    @Override
    protected void apply(PreparedAnimations prepared, ResourceManager resourceManager, ProfilerFiller profiler) {
        long generation = ForeverStudioAnimationRegistry.replace(prepared.documents());
        LOGGER.info("Loaded {} Forever Studio animation document(s), rejected {} invalid document(s), generation {}",
                prepared.documents().size(), prepared.rejected(), generation);
    }

    record PreparedAnimations(Map<String, ForeverStudioAnimationDocument> documents, int rejected) { }
}
