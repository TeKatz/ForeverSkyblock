package de.tekatz.foreverskyblock.mixin;

import de.tekatz.foreverskyblock.client.KMenuEditorScreen;
import de.tekatz.foreverskyblock.editor.EditorSession;
import java.lang.reflect.Field;
import java.util.List;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Published custom quests are tombstoned for progress safety but behave as deleted in the authoring graph. */
@Mixin(KMenuEditorScreen.class)
public abstract class KMenuEditorDeletedGraphMixin {
    @Shadow private EditorSession session;
    @Inject(method="graphIds",at=@At("RETURN"),cancellable=true)
    private void foreverSkyblock$hideDeletedCustomNodes(CallbackInfoReturnable<List<String>> cir){
        if(!"GRAPH".equals(foreverSkyblock$mode()))return;
        cir.setReturnValue(cir.getReturnValue().stream().filter(id->{var q=session.current().quest(id);return q==null||q.enabled()||!id.startsWith("forever_skyblock:quest/");}).toList());
    }
    private String foreverSkyblock$mode(){try{Field f=KMenuEditorScreen.class.getDeclaredField("mode");f.setAccessible(true);return String.valueOf(f.get(this));}catch(ReflectiveOperationException e){return "";}}
}
