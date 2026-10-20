using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PackageV2.SqliteSpike;
using UABS.UnityAssets;

string mode = args.FirstOrDefault() ?? "test";
if(mode=="probe") { Bench.Probe(args[1]); return; }
string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../artifacts", "uabs-v2-spike-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(root);
Console.WriteLine("Disposable output: " + root);
if (mode == "test") await Tests.Run(root);
else if (mode == "bench") await Bench.Run(root, args.Length > 1 ? int.Parse(args[1]) : 10000);
else throw new ArgumentException("Use test or bench [asset count].");

static class Data
{
    public static readonly (int Id, string Type)[] Types = [(28,"Texture2D"),(213,"Sprite"),(43,"Mesh"),(83,"AudioClip"),
        (114,"MonoBehaviour"),(49,"TextAsset"),(1,"GameObject"),(99999,"Unknown")];
    public static IEnumerable<Input> Rows(int count, int package)
    {
        for (int i = 0; i < count; i++)
        {
            long source = i / 1000;
            if (source % 8 != package) continue;
            var type = Types[i % 8];
            string? name = i % 37 == 0 ? null : i % 1009 == 0 ? "HeroIcon" : $"asset_{i % 5000:0000}_{type.Type}";
            string? memo = i % 10 < 6 ? null : i % 10 == 6 ? "" : i % 10 == 7 ? "  " :
                (i % 10 == 8 ? "review approved " : "needs attention ") + string.Concat(Enumerable.Repeat("detail ", 1 + i % 24));
            yield return new(i, source, source * 2 + i % 2, i % 1000 - 500, type.Id, type.Type, name, memo);
        }
    }
    public static readonly (string Label, Query Query)[] Queries = [
        ("exact",new(["heroicon"],Exact:true)), ("substring",new(["hero"])),
        ("include_or",new(["hero","0499"])), ("exclude",new(["texture"],["0040"])),
        ("class",new(Class:43)), ("pathid",new(Path:-77)), ("memo",new(Memo:"approved")),
        ("memo_only",new(MemoOnly:true)), ("combined",new(["asset"],Class:43,Memo:"approved")),
        ("large",new(["asset"])), ("none",new(["impossible_needle"])), ("short",new(["as"]))];
    public static string Hash(IEnumerable<Row> rows)
    {
        using var sha=SHA256.Create();
        using(var stream=new CryptoStream(Stream.Null,sha,CryptoStreamMode.Write))
        { JsonSerializer.Serialize(stream,rows); stream.FlushFinalBlock(); }
        return Convert.ToHexString(sha.Hash!);
    }
    public static object Memory()
    {
        using var process = Process.GetCurrentProcess(); process.Refresh();
        return new { WorkingSetMiB = process.WorkingSet64 / 1048576d, PeakMiB = process.PeakWorkingSet64 / 1048576d,
            ManagedMiB = GC.GetTotalMemory(false) / 1048576d };
    }
    public static double Time(Action action) { var w = Stopwatch.StartNew(); action(); return w.Elapsed.TotalMilliseconds; }
    public static double Median(Func<double> sample) => Enumerable.Range(0,3).Select(_ => sample()).Order().ElementAt(1);
    public static void Output(string root, string file, object value) =>
        File.WriteAllText(Path.Combine(root,file),JsonSerializer.Serialize(value,new JsonSerializerOptions { WriteIndented=true }));
}

static class Tests
{
    private static int checks;
    public static void Check(bool condition, string label)
    { if (!condition) throw new InvalidOperationException(label); checks++; Console.WriteLine("PASS " + label); }

    public static async Task Run(string root)
    {
        string path = Path.Combine(root,"test.db");
        Input[] special = [new(0,0,0,-7,99999,null,null,null),new(1,0,1,-7,43,"Mesh","duplicate",""),
            new(2,1,2,-7,83,"AudioClip","duplicate","before"),new(3,1,3,7,114,"MonoBehaviour","Äpfel", "\u2003"),
            new(4,1,3,8,49,"TextAsset","钩爪图标","Straße"),new(5,1,3,9,1,"GameObject","literal%_name",null)];
        Store.Build(path,"test",special);
        var all = Search.Run(path,new());
        Check(all.Count == special.Length && all.Select(r => r.Key).Distinct().Count() == all.Count,"all classes and logical identities");
        Check(all[0].Name is null && all[0].ClassId == 99999,"unnamed unknown class retained");
        Check(all[0].Memo is null && all[1].Memo == "","missing versus empty memo");
        foreach (var q in Data.Queries.Select(x => x.Query).Concat(new Query[] { new(["äpf"]),new(["ÄPFEL"],Exact:true),
            new(["钩爪"]),new(["%_"]),new(Memo:"STRASSE"),new(MemoOnly:true) }))
        {
            var expected = all.Where(r => Search.Matches(r,q)).ToList();
            Check(Data.Hash(expected) == Data.Hash(Search.Run(path,q,true)),"OrdinalIgnoreCase query " + JsonSerializer.Serialize(q));
        }
        var key = all[0].Key;
        Check(Store.Save(path,key,null,0,"").Status == "Saved","null to empty commit and fresh verification");
        Check(Store.Save(path,key,null,0,"stale").Status == "ConflictOrMissing","stale memo/revision rejected");
        var before = Search.Run(path,new());
        Check(Store.Save(path,key,"",1,"abandoned",rollback:true).Status == "RolledBack" &&
            Data.Hash(before) == Data.Hash(Search.Run(path,new())),"rollback preserves all records");
        Check(Store.Save(path,key with { Generation="stale" },"",1,"wrong").Status == "ConflictOrMissing","generation mismatch rejected");
        using (var c = Store.Open(path))
        {
            Store.Exec(c,"PRAGMA journal_mode=WAL");
            using var blocker = c.BeginTransaction(deferred:false);
            bool busy = false;
            try { Store.Save(path,key,"",1,"blocked"); } catch (SqliteException e) when (e.SqliteErrorCode is 5 or 6) { busy=true; }
            Check(busy,"competing writer busy without mutation"); blocker.Rollback();
        }
        using var start = new ManualResetEventSlim();
        var attempts = Enumerable.Range(0,2).Select(i => Task.Run(() => { start.Wait(); return Store.Save(path,key,"",1,"writer-"+i); })).ToArray();
        start.Set(); var saves = await Task.WhenAll(attempts);
        Check(saves.Count(r => r.Status == "Saved") == 1 && saves.Count(r => r.Status == "ConflictOrMissing") == 1,
            "two competing writers: one commit, one stale conflict");
        Check(Data.Hash(before.Skip(1)) == Data.Hash(Search.Run(path,new()).Skip(1)),"unrelated records unchanged");
        var current = Search.Run(path,new()).Single(r => r.Key == key);
        Check(Store.Save(path,key,current.Memo,current.Revision,null).Status == "Saved","empty/value back to missing");
        Check(Search.Run(path,new(MemoOnly:true)).All(r => !string.IsNullOrWhiteSpace(r.Memo)),"Unicode whitespace memo-only parity");
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel(); bool observed = false;
            try { Search.Run(path,new(),token:cancelled.Token); } catch (OperationCanceledException) { observed=true; }
            Check(observed,"pre-cancelled query rejected");
        }
        using (var c = Store.Open(path))
        using (var cancel = new CancellationTokenSource())
        {
            c.CreateFunction("cancel_once", () => { cancel.Cancel(); return 1; });
            using var registration = cancel.Token.Register(() => SQLitePCL.raw.sqlite3_interrupt(c.Handle));
            using var command = c.CreateCommand();
            command.CommandText = "WITH RECURSIVE x(n) AS(VALUES(0) UNION ALL SELECT n+1 FROM x WHERE n<10000000) SELECT sum(n+cancel_once()) FROM x";
            bool interrupted = false;
            try { command.ExecuteScalar(); } catch (SqliteException e) when (e.SqliteErrorCode == 9) { interrupted=true; }
            Check(interrupted,"active native query interrupted deterministically");
        }
        using (var c = Store.Open(path))
        {
            using var tx = c.BeginTransaction();
            Store.Exec(c,"UPDATE asset SET memo='uncommitted' WHERE id=1",tx);
            Check(File.Exists(path+"-wal") && File.Exists(path+"-shm"),"WAL sidecars during live transaction");
            tx.Rollback();
        }
        Check(!File.Exists(path+"-wal") && !File.Exists(path+"-shm"),"WAL checkpoint/sidecar removal on last close (observed)");
        using (var c = Store.Open(path)) Store.Exec(c,"PRAGMA journal_mode=DELETE");
        using (var c = Store.Open(path))
        {
            using var tx = c.BeginTransaction(); Store.Exec(c,"UPDATE asset SET memo='uncommitted' WHERE id=1",tx);
            Check(File.Exists(path+"-journal"),"rollback journal exists during write"); tx.Rollback();
            Check(Store.Scalar<string>(c,"PRAGMA integrity_check") == "ok","integrity after rollback");
        }
        string moved = Path.Combine(root,"moved.db"); File.Move(path,moved);
        Check(Search.Run(moved,new()).Count == all.Count,"closed/unloaded database moves and reloads");
        string asciiPath = Path.Combine(root,"ascii.db"); Store.Build(asciiPath,"ascii",Data.Rows(10000,0));
        var asciiRows = Search.Run(asciiPath,new());
        foreach (var q in Data.Queries)
            Check(Data.Hash(asciiRows.Where(r=>Search.Matches(r,q.Query))) == Data.Hash(Search.Run(asciiPath,q.Query,true)),"ASCII indexes " + q.Label);
        var selected = new[] { asciiPath };
        Check(Data.Hash(await Search.Federate(selected,new(["asset"]),2,true)) == Data.Hash(Search.Run(asciiPath,new(["asset"]),true)),"federation selected scope");
        var pages = new List<Row>(); long after=-1;
        while (true) { var page=Search.Run(asciiPath,new(),take:31,after:after); if(page.Count==0)break; pages.AddRange(page); after=page[^1].Id; }
        Check(Data.Hash(pages)==Data.Hash(asciiRows),"keyset pages no gaps or duplicates");
        var asciiTarget=asciiRows[0];
        Check(Store.Save(asciiPath,asciiTarget.Key,asciiTarget.Memo,asciiTarget.Revision,"freshmemo").Status=="Saved"
            && Search.Run(asciiPath,new(Memo:"freshmemo"),true).Single().Key==asciiTarget.Key,
            "verified memo updates trigram candidates");
        Check(Store.Save(asciiPath,asciiTarget.Key,"freshmemo",1,"rolledbackmemo",rollback:true).Status=="RolledBack"
            && Search.Run(asciiPath,new(Memo:"rolledbackmemo"),true).Count==0,"rolled-back memo absent from trigram index");
        using(var cancelledScope=new CancellationTokenSource())
        {
            cancelledScope.Cancel();bool cancelled=false;
            try { await Search.Federate(selected,new(),2,true,token:cancelledScope.Token); }
            catch(OperationCanceledException){cancelled=true;}
            Check(cancelled,"cancelled federation returns no partial scope");
        }
        using (var c = Store.Open(asciiPath))
        {
            var probe = new Dictionary<string,object>();
            Store.Exec(c,"CREATE VIRTUAL TABLE words USING fts5(name,tokenize=trigram);INSERT INTO words VALUES('Äpfel'),('as'),('钩爪图标')");
            foreach (string term in new[] { "as","äpf","钩爪" })
            {
                using var cmd=c.CreateCommand();cmd.CommandText="SELECT count(*) FROM words WHERE words MATCH $p";cmd.Parameters.AddWithValue("$p",term);
                probe[term]=cmd.ExecuteScalar()!;
            }
            Data.Output(root,"fts-probes.json",new { Version=Store.Scalar<string>(c,"SELECT sqlite_version()"),MatchCounts=probe });
            // Attachment ceiling of this bundled native build, not a guessed global constant.
            int attached=0;
            try { for(;attached<130;attached++) Store.Exec(c,$"ATTACH DATABASE ':memory:' AS a{attached}"); }
            catch(SqliteException e) { Data.Output(root,"attachment-limit.json",new { Attached=attached,e.Message }); }
            Check(attached>=8 && attached<130,"native attachment limit recorded");
        }
        await Enumerate(root);
        Check(!typeof(AssetsToolsAssetSessionFactory).Assembly.GetReferencedAssemblies().Any(a=>
            a.Name is not null && (a.Name.StartsWith("UnityEngine",StringComparison.Ordinal) || a.Name.StartsWith("UnityEditor",StringComparison.Ordinal))),
            "infrastructure assembly remains Unity-runtime-independent");
        Data.Output(root,"tests.json",new { Passed=checks, Failed=0, Provider=typeof(SqliteConnection).Assembly.GetName().Version?.ToString(), Memory=Data.Memory() });
        Console.WriteLine($"TESTS: {checks} passed, 0 failed");
    }

    private static async Task Enumerate(string root)
    {
        string repo=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        string fixtures=Path.Combine(repo,"UABS.UnityAssets.Tests","Fixtures");
        var evidence=new List<object>();
        foreach(string name in new[]{"aigirl.ab","aigirl.assets"})
        {
            string path=Path.Combine(fixtures,name); byte[] hash=SHA256.HashData(File.ReadAllBytes(path));
            var w=Stopwatch.StartNew(); using var session=await new AssetsToolsAssetSessionFactory().OpenAsync(path);
            double open=w.Elapsed.TotalMilliseconds;
            var logical=session.Assets.Select(a=>a.LogicalFile).Distinct(StringComparer.Ordinal).Select((s,i)=>(s,i)).ToDictionary(x=>x.s,x=>x.i);
            var inputs=session.Assets.Select((a,i)=>new Input(i,0,logical[a.LogicalFile],a.PathId,a.ClassId,a.Type,a.Name,null,path,a.LogicalFile));
            string db=Path.Combine(root,name+".db"); Store.Build(db,name,inputs);
            var rows=Search.Run(db,new());
            Check(rows.Count==session.Assets.Count && rows.Select(r=>r.ClassId).Order().SequenceEqual(session.Assets.Select(a=>a.ClassId).Order()),"fixture all table entries persisted " + name);
            Check(rows.All(r=>r.SourcePath==path) && hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))),"fixture provenance/read-only " + name);
            evidence.Add(new { Fixture=name, Assets=rows.Count, LogicalFiles=session.Metadata.LogicalFileCount, OpenMs=open,
                ClassCounts=rows.GroupBy(r=>r.ClassId).ToDictionary(g=>g.Key,g=>g.Count()),session.Warnings });
        }
        Data.Output(root,"enumeration.json",evidence);
    }
}

static class Bench
{
    public static void Probe(string directory)
    {
        string allowed=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../artifacts"))+Path.DirectorySeparatorChar;
        directory=Path.GetFullPath(directory);
        if(!directory.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Only this experiment's artifacts are allowed.");
        var paths=Enumerable.Range(0,8).Select(p=>Path.Combine(directory,$"r2-p{p}.db")).ToArray();
        double elapsed=Data.Time(()=>paths.SelectMany(p=>Search.Run(p,new(["hero"]),true)).ToList());
        double pagePlain=Data.Median(()=>Data.Time(()=>Search.Run(paths[0],new(["asset"]),false,100)));
        double pageFts=Data.Median(()=>Data.Time(()=>Search.Run(paths[0],new(["asset"]),true,100)));
        Console.WriteLine(JsonSerializer.Serialize(new { ProcessColdSelectiveMs=elapsed,
            Plain100MedianMs=pagePlain,Fts100MedianMs=pageFts,Memory=Data.Memory() }));
    }

    public static async Task Run(string root,int count)
    {
        if(count is <10000 or >1000000)throw new ArgumentOutOfRangeException(nameof(count));
        var paths=Enumerable.Range(0,8).Select(p=>Path.Combine(root,$"p{p}.db")).ToArray();
        // Three complete fresh databases per size, no overwrite of existing packages.
        var insert=new List<double>();var indexes=new List<double>();var fts=new List<double>();
        for(int repetition=0;repetition<3;repetition++)
        {
            double a=0,b=0,c=0;
            for(int p=0;p<8;p++)
            {
                string path=Path.Combine(root,$"r{repetition}-p{p}.db");
                var build=Store.Build(path,"pkg-"+p,Data.Rows(count,p));a+=build.InsertMs;b+=build.IndexMs;c+=build.FtsMs;
                if(repetition==2)paths[p]=path;
            }
            insert.Add(a);indexes.Add(b);fts.Add(c);
        }
        Console.WriteLine($"Created {count:N0} assets across 8 packages.");
        var before=Data.Memory();
        var all=(await Search.Federate(paths,new(),1,false)).OrderBy(r=>r.Id).ToList();
        var loaded=Data.Memory();
        List<object> results=[];
        foreach(var (label,q) in Data.Queries)
        {
            var expected=all.Where(r=>Search.Matches(r,q)).ToList();string hash=Data.Hash(expected);
            double first=0,plain=0,trigram=0,scan=0;
            List<Row> found=[];
            first=Data.Time(()=>found=paths.SelectMany(p=>Search.Run(p,q,true)).OrderBy(r=>r.Id).ToList());
            Tests.Check(Data.Hash(found)==hash,"benchmark first " + label);
            plain=Data.Median(()=>Data.Time(()=>found=paths.SelectMany(p=>Search.Run(p,q)).OrderBy(r=>r.Id).ToList()));
            Tests.Check(Data.Hash(found)==hash,"benchmark B-tree " + label);
            trigram=Data.Median(()=>Data.Time(()=>found=paths.SelectMany(p=>Search.Run(p,q,true)).OrderBy(r=>r.Id).ToList()));
            Tests.Check(Data.Hash(found)==hash,"benchmark trigram " + label);
            scan=Data.Median(()=>Data.Time(()=>found=all.Where(r=>Search.Matches(r,q)).Select(r=>r with {}).ToList()));
            results.Add(new { Query=label,Count=expected.Count,FirstConnectionMs=first,PlainMedianMs=plain,FtsMedianMs=trigram,InMemoryMedianMs=scan,Hash=hash });
        }
        Query selective=new(["hero"]),broad=new(["asset"]);
        var federation=new List<object>();
        foreach(var q in new[]{selective,broad})
        foreach(int workers in new[]{1,2,4})
        {
            List<Row> found=[];var samples=new List<double>();
            for(int r=0;r<3;r++){var w=Stopwatch.StartNew();found=await Search.Federate(paths,q,workers,true);samples.Add(w.Elapsed.TotalMilliseconds);}
            Tests.Check(Data.Hash(found)==Data.Hash(all.Where(r=>Search.Matches(r,q))),"federated ordering/provenance");
            federation.Add(new { Query=q.Include![0],Workers=workers,MedianMs=samples.Order().ElementAt(1),Count=found.Count });
        }
        double attach=Data.Median(()=>Data.Time(()=>Search.Attached(paths,selective)));
        Tests.Check(Data.Hash(Search.Attached(paths,selective))==Data.Hash(all.Where(r=>Search.Matches(r,selective))),"attached parity");
        double bounded=Data.Median(()=>Data.Time(()=>Search.Run(paths[0],broad,true,100)));
        double save=Data.Median(()=>Data.Time(()=>
        {
            var r=Search.Run(paths[0],new(Path:-500)).First();
            Tests.Check(Store.Save(paths[0],r.Key,r.Memo,r.Revision,"memo-"+Guid.NewGuid()).Status=="Saved","benchmark verified save");
        }));
        using var connection=Store.Open(paths[0]);
        var output=new { Assets=count,Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Provider=typeof(SqliteConnection).Assembly.GetName().Version?.ToString(),NativeSqlite=Store.Scalar<string>(connection,"SELECT sqlite_version()"),
            Build=new { InsertMedianMs=insert.Order().ElementAt(1),IndexMedianMs=indexes.Order().ElementAt(1),FtsMedianMs=fts.Order().ElementAt(1) },
            Bytes=paths.Sum(p=>new FileInfo(p).Length),Queries=results,Federation=federation,AttachedSelectiveMedianMs=attach,
            PerPackage100MedianMs=bounded,VerifiedMemoMedianMs=save,MemoryBeforeMaterialization=before,MemoryLoaded=loaded,MemoryFinal=Data.Memory() };
        Data.Output(root,"benchmark.json",output);Console.WriteLine(JsonSerializer.Serialize(output.Build));
        Console.WriteLine($"Verified memo median: {save:F2} ms; bounded 100: {bounded:F2} ms");
    }
}
