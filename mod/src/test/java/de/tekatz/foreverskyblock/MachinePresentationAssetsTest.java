package de.tekatz.foreverskyblock;

import com.google.gson.JsonParser;
import java.io.InputStreamReader;
import javax.imageio.ImageIO;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class MachinePresentationAssetsTest {
    @Test void materialAtlasesKeepSixIndependentCellsAndGlazingHasRealCutoutTransparency() throws Exception {
        for(String name:new String[]{"machine_materials","machine_materials_ii","ascension_materials"}) {
            var image=ImageIO.read(getClass().getResourceAsStream("/assets/forever_skyblock/textures/block/"+name+".png"));
            assertEquals(image.getWidth()/3,image.getHeight()/2);
            var colors=new java.util.HashSet<Integer>();
            for(int i=0;i<6;i++)colors.add(image.getRGB(i%3*32+16,i/3*32+16));
            assertEquals(6,colors.size(),"Each existing UV cell must still identify its own material");
        }
        var glass=ImageIO.read(getClass().getResourceAsStream("/assets/forever_skyblock/textures/block/garden_glass.png"));
        assertEquals(0,glass.getRGB(16,16)>>>24,"The rooted crop must remain visible through the enclosure");
        assertEquals(255,glass.getRGB(0,0)>>>24);
    }
    @Test void everyNewMachineMaterialBindingResolvesToAPackagedTexture() throws Exception {
        for(String name:new String[]{"condenser_mark_i","condenser_mark_ii","ascension_chamber","mixing_chamber_mark_i","freezing_chamber_mark_i","garden_chamber_mark_i","washing_basin","crucible","refining_chamber"}) {
            var path="/assets/forever_skyblock/models/block/"+name+".json";
            try(var stream=getClass().getResourceAsStream(path)) {
                assertNotNull(stream,path);
                var model=JsonParser.parseReader(new InputStreamReader(stream)).getAsJsonObject();
                for(var entry:model.getAsJsonObject("textures").entrySet()) {
                    String location=entry.getValue().getAsString();
                    if(!location.startsWith("forever_skyblock:"))continue;
                    String texture="/assets/forever_skyblock/textures/"+location.split(":",2)[1]+".png";
                    try(var image=getClass().getResourceAsStream(texture)) { assertNotNull(image,texture);assertNotNull(ImageIO.read(image),texture); }
                }
            }
        }
    }
}
