package de.tekatz.foreverskyblock.editor;

import de.tekatz.foreverskyblock.progression.*;
import java.util.*;
import static de.tekatz.foreverskyblock.editor.EditorDocument.*;

/** Pure quest reducers. Only authoritative server events can reach the persistence bridge. */
public final class QuestRules {
    private QuestRules() { }
    public record Event(TaskType type,String target,int count,String identity) {
        public Event { if(type==null||count<1||count>1_000_000||target==null||identity==null||identity.length()>100)throw new IllegalArgumentException("Invalid quest event"); }
    }
    public static boolean unlocked(Quest q,ProgressionProfile p){return q.enabled()&&p.meta().shopTier()>=q.requiredTier()&&dependencies(q.requires(),q.prerequisiteMatch(),p.milestones());}
    public static boolean dependencies(List<String> requires,Match mode,Set<String> completed){return requires.isEmpty()||(mode==Match.ALL?requires.stream().allMatch(completed::contains):requires.stream().anyMatch(completed::contains));}
    public static Map<String,Integer> counts(Quest q,ProgressionProfile p) {
        var map=new LinkedHashMap<String,Integer>();var state=p.meta().progress().get(q.id());
        for(var task:q.tasks()) {
            int count=0;
            if(state!=null) {
                if(q.legacy()&&state.revision()==1)count=state.evidence().size();
                else if(state.revision()==2)for(String token:state.evidence())if(token.startsWith("g/"+task.id()+"="))try{count=Integer.parseInt(token.substring(token.indexOf('=')+1));}catch(NumberFormatException ignored){}
            }
            map.put(task.id(),q.legacy()&&p.milestones().contains(q.id())?task.count():Math.min(task.count(),Math.max(0,count)));
        }
        return map;
    }
    public static ProgressionProfile record(ProgressionProfile profile,EditorDocument document,Event event) {
        // All unlock checks use the profile before this event. A prerequisite cannot replay one action into its child.
        var before=profile;
        for(var q:document.quests()) {
            if(q.legacy()||before.milestones().contains(q.id())||!unlocked(q,before))continue;
            var counts=counts(q,profile);var old=profile.meta().progress().get(q.id());
            if(old!=null&&old.revision()!=2)continue;
            if(old!=null&&!event.identity().isEmpty()&&old.evidence().stream().anyMatch(s->s.equals("seen/"+event.identity())||s.startsWith("seen/")&&s.endsWith("/"+event.identity())))continue;
            boolean changed=false,matched=false;
            for(var task:q.tasks())if(task.type()==event.type()&&(task.target().equals(event.target())||task.type()==TaskType.KILL&&task.target().equals("minecraft:any_hostile")&&event.identity().startsWith("h/"))) {
                matched=true;
                int next=(int)Math.min(task.count(),(long)counts.get(task.id())+event.count());
                if(next!=counts.get(task.id())){counts.put(task.id(),next);changed=true;}
            }
            if(!changed){if(matched&&complete(q,counts))profile=award(profile,q.id(),q.rewardSp(),q.rewardPp());continue;}
            var evidence=new LinkedHashSet<String>();counts.forEach((key,n)->evidence.add("g/"+key+"="+n));
            if(!event.identity().isEmpty()){
                long sequence=old==null?0:old.evidence().stream().filter(s->s.startsWith("seen/")).mapToLong(QuestRules::sequence).max().orElse(0);
                evidence.add("seen/"+Math.addExact(sequence,1)+"/"+event.identity());
                if(old!=null)old.evidence().stream().filter(s->s.startsWith("seen/")).sorted(Comparator.comparingLong(QuestRules::sequence).reversed()).limit(31).forEach(evidence::add);
            }else if(old!=null)old.evidence().stream().filter(s->s.startsWith("seen/")).forEach(evidence::add);
            var progress=new LinkedHashMap<>(profile.meta().progress());progress.put(q.id(),new MetaProgress.Progress(2,evidence));profile=profile.withMeta(profile.meta().withProgress(progress));
            if(complete(q,counts))profile=award(profile,q.id(),q.rewardSp(),q.rewardPp());
        }
        return profile;
    }
    public static ProgressionProfile recordBatch(ProgressionProfile profile,EditorDocument document,List<Event> events){for(var event:events)profile=record(profile,document,event);return profile;}
    private static long sequence(String token){try{int slash=token.indexOf('/',5);return slash<0?0:Long.parseLong(token.substring(5,slash));}catch(NumberFormatException e){return 0;}}
    private static boolean complete(Quest q,Map<String,Integer> counts){return q.taskMatch()==Match.ALL?q.tasks().stream().allMatch(t->counts.get(t.id())>=t.count()):q.tasks().stream().anyMatch(t->counts.get(t.id())>=t.count());}
    public static ProgressionProfile award(ProgressionProfile p,String id,int sp,int pp) {
        if(p.milestones().contains(id))return p;
        var done=new LinkedHashSet<>(p.milestones());done.add(id);
        return new ProgressionProfile(Math.addExact(p.points(),sp),done,p.skills(),Math.addExact(p.perkPoints(),pp),p.perkInvestments(),p.redeemedCores(),p.meta().account(sp,pp,0,0,0,0,0));
    }
}
