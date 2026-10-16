package de.tekatz.foreverskyblock;

import com.google.gson.*;
import java.io.InputStreamReader;
import java.util.*;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

final class AscensionModelTest {
    private record Face(int axis,double plane,boolean positive,double[] lo,double[] hi) { }
    private static double[] vector(JsonArray a) { return new double[]{a.get(0).getAsDouble(),a.get(1).getAsDouble(),a.get(2).getAsDouble()}; }
    @Test void bothRecessedHousingsHaveDisjointExteriorFacesAndOnlyBoundaryCulling() throws Exception {
        for(String name:List.of("condenser_mark_i","ascension_chamber")) for(String suffix:List.of("","_active")) {
            try(var stream=getClass().getResourceAsStream("/assets/forever_skyblock/models/block/"+name+suffix+".json")) {
                assertNotNull(stream);var model=JsonParser.parseReader(new InputStreamReader(stream)).getAsJsonObject();
                List<Face> faces=new ArrayList<>();double volume=0,backArea=0;
                assertTrue(model.getAsJsonArray("elements").size()<180,"Greedy meshing must not retain thousands of voxel faces");
                for(var raw:model.getAsJsonArray("elements")) {
                    var e=raw.getAsJsonObject();double[] lo=vector(e.getAsJsonArray("from")),hi=vector(e.getAsJsonArray("to"));
                    for(int a=0;a<3;a++)assertTrue(lo[a]>=0&&hi[a]<=16&&lo[a]<=hi[a]);
                    for(var entry:e.getAsJsonObject("faces").entrySet()) {
                        String d=entry.getKey();int axis=switch(d){case "east","west"->0;case "up","down"->1;default->2;};
                        boolean positive=List.of("east","up","south").contains(d);double plane=lo[axis];assertEquals(plane,hi[axis]);
                        double area=1;for(int a=0;a<3;a++)if(a!=axis){assertTrue(hi[a]>lo[a]);area*=hi[a]-lo[a];}
                        var face=entry.getValue().getAsJsonObject();if(face.has("cullface")){assertEquals(d,face.get("cullface").getAsString());assertEquals(positive?16:0,plane);}
                        for(var uv:face.getAsJsonArray("uv"))assertTrue(uv.getAsDouble()>=0&&uv.getAsDouble()<=16);
                        faces.add(new Face(axis,plane,positive,lo,hi));volume+=(positive?1:-1)*plane*area/3;
                        if(d.equals("south")&&plane==16)backArea+=area;
                    }
                }
                assertEquals(256,backArea,.00001,"The chamber must have a fully closed solid rear wall");
                assertTrue(volume>3000&&volume<4096,"A real front recess must remain inside an otherwise solid housing");
                for(int i=0;i<faces.size();i++)for(int j=i+1;j<faces.size();j++) {
                    Face a=faces.get(i),b=faces.get(j);if(a.axis()!=b.axis()||a.plane()!=b.plane())continue;
                    boolean overlap=true;for(int axis=0;axis<3;axis++)if(axis!=a.axis())overlap&=Math.min(a.hi()[axis],b.hi()[axis])-Math.max(a.lo()[axis],b.lo()[axis])>1e-6;
                    assertFalse(overlap,"Coplanar overlaps cause flickering faces: "+name);
                }
            }
        }
    }
}
