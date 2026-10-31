insert into public.booth_viewers(owner_hash,token_hash,enabled) values(repeat('d',64),repeat('c',64),true);
insert into public.booth_sessions(id,owner_hash,photo_count,mode,style,published_at,used_original_fallback)
select ('f1000000-0000-4000-8000-'||lpad(n::text,12,'0'))::uuid,repeat('d',64),case when n in(2,3,4) then 4 else 1 end,
 case when n in(2,3,4) then 'surprise' else 'original' end,'halloween',now(),n=3
from generate_series(1,6) n;
insert into public.booth_assets(session_id,name,object_key,sha256,bytes,uploaded)
select s.id,'photo-'||i||'.jpg','viewer-test/'||s.id||'/photo-'||i||'.jpg',repeat('a',64),100,true
from public.booth_sessions s cross join generate_series(1,4) i
where s.owner_hash=repeat('d',64) and i<=s.photo_count and not(s.id='f1000000-0000-4000-8000-000000000004' and i>1);
insert into public.booth_assets(session_id,name,object_key,sha256,bytes,uploaded)
values('f1000000-0000-4000-8000-000000000002','original-1.jpg','viewer-test/DO-NOT-PROJECT.jpg',repeat('b',64),100,true);
insert into public.booth_projections(session_id,source)
select id,case when id='f1000000-0000-4000-8000-000000000006' then 'fixture' else 'camera' end
from public.booth_sessions where owner_hash=repeat('d',64) and id<>'f1000000-0000-4000-8000-000000000005';
do $$ declare result jsonb; begin
 result:=public.booth_viewer_feed(repeat('d',64));
 assert jsonb_array_length(result)=2,'Only ready released non-test sessions';
 assert result::text not like '%DO-NOT-PROJECT%','No archived originals';
 assert result::text not like '%000000000003%' and result::text not like '%000000000004%','No surprise fallback or partial results';
 assert jsonb_array_length(public.booth_viewer_feed(repeat('e',64)))=0,'Owner isolation';
 assert not has_table_privilege('anon','public.booth_viewers','SELECT'),'Private viewer capability';
 assert not has_table_privilege('authenticated','public.booth_projections','SELECT'),'Private release queue';
 assert not has_function_privilege('anon','public.booth_viewer_feed(text)','EXECUTE'),'No public RPC';
end $$;
update public.booth_viewers set include_tests=true where owner_hash=repeat('d',64);
do $$ begin assert jsonb_array_length(public.booth_viewer_feed(repeat('d',64)))=3,'Explicit test inclusion'; end $$;
update public.booth_viewers set enabled=false where owner_hash=repeat('d',64);
do $$ begin assert jsonb_array_length(public.booth_viewer_feed(repeat('d',64)))=0,'Disable stops all projection'; end $$;
