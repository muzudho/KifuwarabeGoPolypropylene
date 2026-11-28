namespace KifuwarabeGoPolypropylene.ThinkEngine;

internal static class SelfTest
{
    public static void Run()
    {
        int count = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new InvalidOperationException("FAIL: " + name);
            count++;
        }
        // 戦術#18：実戦1706435の白8F3。最終同順位で隣接伸びE3よりコスミを検討する。
        int[] expansionHistory = {29,50,33,31,47,24,21};
        for (int swap=0; swap<2; swap++)
        for (int symmetry=0; symmetry<8; symmetry++)
        {
            int MapExpansion(int p) { int x=p%9,y=p/9; if(symmetry>=4)x=8-x; for(int k=0;k<symmetry%4;k++)(x,y)=(y,8-x); return y*9+x; }
            int ExpansionColor(int c)=>swap==0?c:3-c;
            Board ExpansionReplay() { var b=new Board(9); for(int i=0;i<expansionHistory.Length;i++)Check(b.Play(ExpansionColor(i%2+1),MapExpansion(expansionHistory[i])),"expansion history"); return b; }
            var root=ExpansionReplay(); string before=root.PositionKey;
            Check(root.IsOpeningKosumiExpansion(ExpansionColor(2),MapExpansion(23)),"expansion kosumi accepted");
            Check(!root.IsOpeningKosumiExpansion(ExpansionColor(2),MapExpansion(22)),"adjacent stretch not kosumi");
            Check(root.PositionKey==before&&root.MoveCount==7,"expansion nonmutating");
            if(swap==0&&symmetry==0)Check(root.Generate(2)==23,"actual opponent expansion reproduced");
            foreach(int bridge in new[]{22,32})
            {
                var blocked=ExpansionReplay();
                Check(blocked.Play(ExpansionColor(2),null),"bridge setup pass");
                Check(blocked.Play(ExpansionColor(1),MapExpansion(bridge)),"bridge occupied");
                Check(!blocked.IsOpeningKosumiExpansion(ExpansionColor(2),MapExpansion(23)),"occupied bridge not expansion");
            }
            var enclosed=new Board(9); Check(enclosed.Play(ExpansionColor(1),MapExpansion(29)),"enclosed kosumi anchor");
            foreach(int p in new[]{28,38,19,11,31,39})Check(enclosed.Play(ExpansionColor(2),MapExpansion(p)),"enclosed kosumi boundary");
            Check(!enclosed.IsOpeningKosumiExpansion(ExpansionColor(1),MapExpansion(21)),"narrow connection not expansion");
            foreach(int moveCount in new[]{3,24,398,399})
            {
                var boundary=ExpansionReplay();
                typeof(Board).GetProperty(nameof(Board.MoveCount))!.SetValue(boundary,moveCount);
                Check(!boundary.IsOpeningKosumiExpansion(ExpansionColor(2),MapExpansion(23)),"expansion move window");
            }
            Check(!new Board(19).IsOpeningKosumiExpansion(ExpansionColor(2),MapExpansion(23)),"expansion restricted to nine");
        }
        // 戦術#20：実戦1706363白58F2。別の連が二眼でも通常の団子回避を解除しない。
        int?[] postLifeGame = { 48, 29, 33, 51, 41, 21, 50, 22, 60, 23, 37, 24, 28, 38, 47, 42, 52, 43, 34, 44, 53, 56, 55, 57, 64, 58, 35, 49, 40, 59, 39, 20, 68, 10, 67, 16, 19, 70, 69, 61, 9, 11, 66, 3, 25, 4, 17, 13, 45, 1, 46, 0, 18, 5, 7, 6, 15 };
        foreach (int postLifeColor in new[] { 1, 2 })
        for (int postLifeSym = 0; postLifeSym < 8; postLifeSym++)
        {
            int PostLifeMap(int p)
            {
                int x = p % 9, y = p / 9;
                if (postLifeSym >= 4) x = 8 - x;
                for (int i = 0; i < postLifeSym % 4; i++) (x, y) = (y, 8 - x);
                return y * 9 + x;
            }
            var postLife = new Board(9);
            for (int i = 0; i < postLifeGame.Length; i++)
            {
                int originalColor = i % 2 + 1;
                int color = originalColor == 2 ? postLifeColor : 3 - postLifeColor;
                int? point = postLifeGame[i] is int p ? PostLifeMap(p) : null;
                Check(postLife.Play(color, point), "post-life full history legal");
            }
            Check(Enumerable.Range(0, 81).Any(p => postLife.At(p % 9, p / 9) == postLifeColor
                && postLife.HasTwoEyes(postLifeColor, p)), "post-life already has two eyes");
            int target = PostLifeMap(14);
            string before = postLife.PositionKey;
            Check(postLife.ShouldDeferDumpling(postLifeColor, target), "post-life target is deferred dumpling");
            Check(postLife.PositionKey == before, "post-life shape check does not mutate");
            int? selected = postLife.Generate(postLifeColor);
            Check(selected.HasValue && selected != target, "post-life prefers alternative to dumpling");
            Check(selected is int q && !KifuwarabeGoPolypropylene.Domain.LocalPatterns.DumplingStones
                .DumplingStonesPattern.Matches(before.Select(c => c - '0').ToArray(), 9, postLifeColor, q),
                "post-life alternative is not another dumpling");
        }
        // 戦術#31：一眼を作っても取られる実戦と、反撃できる二呼吸点の負例。
        int?[] trappedEyeHistory = { 50,29,57,33,31,21,22,23,12,20,14,24,15,38,43,42,51,47,16,56,66,58,49,59,60,10,65,61,52,64,68,70,13,69,34,25,26,0,67,1,73,55,39,48,30,8,77,7,53,6,5,18,11,2,3,19,63,36,74,37,62,45,40,54,72,71,17 };
        for (int swap = 0; swap < 2; swap++)
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            int MapEye(int p)
            {
                int x=p%9,y=p/9;
                if(symmetry>=4)x=8-x;
                for(int k=0;k<symmetry%4;k++)(x,y)=(y,8-x);
                return x+9*y;
            }
            int owner=swap==0?2:1;
            var trapped=new Board(9);
            for(int i=0;i<trappedEyeHistory.Length;i++)
            {
                int color=i%2+1;if(swap==1)color=3-color;
                Check(trapped.Play(color,trappedEyeHistory[i] is int p?MapEye(p):null),"trapped eye full history");
            }
            int target=MapEye(79);string before=trapped.PositionKey;
            Check(trapped.FormsSinglePointEye(owner,target),"trapped one eye detected");
            Check(!trapped.ShouldPrioritizeSinglePointEye(owner,target),"bounded capture removes eye priority");
            Check(trapped.PositionKey==before,"eye reading does not mutate");
            Check(trapped.Generate(owner)!=target,"prefer other move to trapped eye");
            int defender=swap==0?1:2;var counter=new Board(9);
            foreach(int p in new[]{1,10,3,12,21,29,28})Check(counter.Play(defender,MapEye(p)),"countercapture friendly setup");
            foreach(int p in new[]{2,11,20,19})Check(counter.Play(3-defender,MapEye(p)),"countercapture enemy setup");
            before=counter.PositionKey;
            Check(counter.FormsSinglePointEye(defender,MapEye(9)),"countercapture one eye detected");
            Check(counter.ShouldPrioritizeSinglePointEye(defender,MapEye(9)),"retain eye priority with countercapture");
            Check(counter.PositionKey==before,"countercapture reading does not mutate");
            Check(counter.Play(defender,MapEye(9)),"countercapture eye move");
            Check(counter.Play(3-defender,MapEye(18)),"countercapture opponent attack");
            Check(counter.Play(defender,MapEye(27)),"countercapture defense");
            foreach(int p in new[]{1,10,9})Check(counter.At(MapEye(p)%9,MapEye(p)/9)==defender,"countercapture retains original stones");
        }
        // 戦術#18：全履歴でコスミの橋と割込み後の再連絡を確認。未確認の長期生きとは区別する。
        int[] kosumiOpening={29,50,33,57,21,31,22,32,23,42,24,47,38,46,49,58};
        for(int swap=0;swap<2;swap++)for(int symmetry=0;symmetry<8;symmetry++)
        {
            int MapKosumi(int p){int x=p%9,y=p/9;if(symmetry>=4)x=8-x;for(int i=0;i<symmetry%4;i++)(x,y)=(y,8-x);return x+9*y;}
            int owner=swap==0?1:2;
            Board KosumiReplay(){var b=new Board(9);for(int i=0;i<kosumiOpening.Length;i++){int color=i%2+1;if(swap==1)color=3-color;Check(b.Play(color,MapKosumi(kosumiOpening[i])),"kosumi full history");}return b;}
            var candidate=KosumiReplay();int target=MapKosumi(39);string key=candidate.PositionKey;
            Check(candidate.IsLibertyImprovingKosumiConnection(owner,target),"kosumi two bridges recover");
            Check(candidate.HasRecoverableSplitWithKosumi(owner,target),"kosumi split recovers");
            Check(!candidate.ShouldDeferEmptyTriangle(owner,target),"necessary kosumi triangle exception");
            Check(!candidate.ShouldDeferOneSpaceSplit(owner,target),"recoverable kosumi split exception");
            Check(candidate.PositionKey==key,"kosumi checks nonmutating");
            if(swap==0&&symmetry==0)Check(candidate.Generate(owner)==target,"kosumi actual root chooses D5");
            var blocked=KosumiReplay();Check(blocked.Play(owner,null),"kosumi blocked setup pass");Check(blocked.Play(3-owner,MapKosumi(48)),"kosumi bridge occupied");
            Check(!blocked.IsLibertyImprovingKosumiConnection(owner,target),"occupied kosumi bridge rejected");
            Check(!blocked.HasRecoverableSplitWithKosumi(owner,target),"occupied bridge split exception rejected");
            var plain=new Board(9);Check(plain.Play(owner,MapKosumi(29)),"bare split friendly");Check(plain.Play(3-owner,MapKosumi(39)),"bare split enemy");
            Check(plain.ShouldDeferOneSpaceSplit(owner,MapKosumi(31)),"bare split still deferred");
            Check(!plain.HasRecoverableSplitWithKosumi(owner,MapKosumi(31)),"bare split no kosumi exception");
            foreach(int moves in new[]{397,398,399})
            {
                var terminal=KosumiReplay();
                // 盤面を保ち400手境界だけを人工的に再現する。
                typeof(Board).GetProperty(nameof(Board.MoveCount))!.SetValue(terminal,moves);
                Check(terminal.IsLibertyImprovingKosumiConnection(owner,target)==(moves==397),"kosumi no imaginary post-limit reply");
                Check(terminal.HasRecoverableSplitWithKosumi(owner,target)==(moves==397),"split no imaginary post-limit reply");
                Check(terminal.MoveCount==moves,"kosumi limit checks nonmutating");
            }
        }
        LocalPatternSelfTest.Run(Check);
        // 戦術#25の反例：一石取りと別連アタリでも、十一石を取り返されるB5は選ばない。
        var pressureReversalGame = new (int Color, int? Point)[] { (1, 29), (2, 50), (1, 33), (2, 32), (1, 47), (2, 24), (1, 21), (2, 57), (1, 22), (2, 23), (1, 38), (2, 60), (1, 42), (2, 41), (1, 51), (2, 34), (1, 56), (2, 65), (1, 58), (2, 67), (1, 59), (2, 49), (1, 68), (2, 69), (1, 77), (2, 76), (1, 14), (2, 15), (1, 16), (2, 13), (1, 25), (2, 52), (1, 64), (2, 48), (1, 70), (2, 12), (1, 10), (2, 11), (1, 61), (2, 43), (1, 0), (2, 20), (1, 19), (2, 30), (1, 39), (2, 31), (1, 40), (2, 73), (1, 5), (2, 78), (1, 4), (2, 3), (1, 8), (2, 63), (1, 27), (2, 55), (1, 36), (2, 46), (1, 45), (2, 6), (1, 54), (2, 7), (1, 79), (2, 18), (1, 71), (2, 28) };
        // 戦術#27：実戦1706252の42手後。二呼吸点の二石を、六呼吸点の四石へつなぐ。
        foreach (int connectionColor in new[] { 1, 2 })
        for (int connectionSym = 0; connectionSym < 8; connectionSym++)
        {
            int ConnectionMap(int p)
            {
                int x = p % 9, y = p / 9;
                if (connectionSym >= 4) x = 8 - x;
                for (int i = 0; i < connectionSym % 4; i++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            var connectionRoot = new Board(9);
            foreach (var move in pressureReversalGame.Take(42))
                Check(connectionRoot.Play(connectionColor == 1 ? move.Color : 3 - move.Color,
                    move.Point is int p ? ConnectionMap(p) : null), "tactic#27 full history");
            int target = ConnectionMap(30);
            string beforeConnection = connectionRoot.PositionKey;
            Check(connectionRoot.IsLibertyImprovingConnection(connectionColor, target), "tactic#27 connection detected");
            Check(!connectionRoot.ShouldDeferEmptyTriangle(connectionColor, target), "tactic#27 necessary triangle allowed");
            Check(beforeConnection == connectionRoot.PositionKey, "tactic#27 detection preserves root");
            Check(connectionRoot.Generate(connectionColor) == target, "tactic#27 selects connecting D4");
            var ordinary = new Board(9);
            foreach (int p in new[] { 29, 21 }) Check(ordinary.Play(connectionColor, ConnectionMap(p)), "tactic#27 ordinary setup");
            Check(!ordinary.IsLibertyImprovingConnection(connectionColor, ConnectionMap(20)), "tactic#27 no weak chain exception");
            Check(ordinary.ShouldDeferEmptyTriangle(connectionColor, ConnectionMap(20)), "tactic#27 ordinary triangle still deferred");
            Check(ordinary.Play(connectionColor, ConnectionMap(30)), "tactic#27 same chain setup");
            Check(!ordinary.IsLibertyImprovingConnection(connectionColor, ConnectionMap(20)), "tactic#27 already connected excluded");
            Check(!new Board(19).IsLibertyImprovingConnection(connectionColor, 60), "tactic#27 other board size excluded");
        }
        foreach (int reversalColor in new[] { 1, 2 })
        for (int reversalSym = 0; reversalSym < 8; reversalSym++)
        {
            int ReversalMap(int p)
            {
                int x = p % 9, y = p / 9;
                if (reversalSym >= 4) x = 8 - x;
                for (int i = 0; i < reversalSym % 4; i++) (x, y) = (y, 8 - x);
                return x + y * 9;
            }
            var pressureReversalRoot = new Board(9);
            var pressureReversalTrial = new Board(9);
            foreach (var move in pressureReversalGame)
            {
                int c = reversalColor == 1 ? move.Color : 3 - move.Color;
                int? q = move.Point is int point ? ReversalMap(point) : null;
                Check(pressureReversalRoot.Play(c, q) && pressureReversalTrial.Play(c, q), "tactic#25 reversal history");
            }
            int bad = ReversalMap(37);
            Check(pressureReversalRoot.CaptureAndAtariPressure(reversalColor, bad) == 2,
                "tactic#25 pattern alone also matches losing capture");
            Check(pressureReversalTrial.Play(reversalColor, bad), "tactic#25 bad capture legal");
            Check(pressureReversalTrial.StonesLostToImmediateReply(reversalColor, bad) == 11,
                "tactic#25 bad capture loses eleven");
            Check(pressureReversalRoot.Generate(reversalColor) != bad, "tactic#25 does not choose losing pressure");
        }

        // 戦術#25：捕獲と別連アタリの実選択、および二眼完成を先に守る実戦反例。
        var pressureGames = new[] { new (int Color, int? Point)[] { (1, 48), (2, 29), (1, 30), (2, 20), (1, 38), (2, 21), (1, 31), (2, 22), (1, 32), (2, 23), (1, 33), (2, 24), (1, 68), (2, 41), (1, 43), (2, 42), (1, 25), (2, 47), (1, 56), (2, 46), (1, 37), (2, 49), (1, 57), (2, 58), (1, 67), (2, 10), (1, 60), (2, 16), (1, 34), (2, 52), (1, 51), (2, 55), (1, 64), (2, 61), (1, 70), (2, 65), (1, 45), (2, 66), (1, 54), (2, 46), (1, 15), (2, 0), (1, 14), (2, 2), (1, 7), (2, 3), (1, 28), (2, 5), (1, 17), (2, 27), (1, 19), (2, 18), (1, 40), (2, 35), (1, 55), (2, 44), (1, 53), (2, 62), (1, 78), (2, 71), (1, 13), (2, 4), (1, 12), (2, 11) }, new (int Color, int? Point)[] { (1, 50), (2, 29), (1, 48), (2, 33), (1, 31), (2, 21), (1, 22), (2, 23), (1, 13), (2, 38), (1, 14), (2, 42), (1, 51), (2, 47), (1, 25), (2, 56), (1, 57), (2, 58), (1, 66), (2, 49), (1, 40), (2, 59), (1, 60), (2, 16), (1, 15), (2, 52), (1, 68), (2, 61), (1, 70), (2, 64), (1, 34), (2, 43), (1, 24), (2, 7), (1, 12), (2, 17), (1, 11), (2, 10), (1, 35), (2, 0), (1, 39), (2, 6), (1, 32), (2, 41), (1, 69), (2, 5), (1, 19), (2, 4), (1, 20), (2, 30), (1, 28), (2, 3), (1, 37), (2, 2), (1, 46), (2, 26), (1, 44), (2, 63), (1, 53), (2, 73), (1, 18), (2, 80), (1, 55), (2, 65), (1, 9), (2, 54) } };
        for (int pressureCase = 0; pressureCase < pressureGames.Length; pressureCase++)
        for (int pressureSwap = 0; pressureSwap < 2; pressureSwap++)
        for (int pressureSym = 0; pressureSym < 8; pressureSym++)
        {
            int PressureMap(int p)
            {
                int x = p % 9, y = p / 9;
                if (pressureSym >= 4) x = 8 - x;
                for (int i = 0; i < pressureSym % 4; i++) (x, y) = (y, 8 - x);
                return x + y * 9;
            }
            var pressureRoot = new Board(9);
            foreach (var move in pressureGames[pressureCase])
                Check(pressureRoot.Play(pressureSwap == 0 ? move.Color : 3 - move.Color,
                    move.Point is int q ? PressureMap(q) : null), "tactic#25 full history");
            int own = pressureSwap == 0 ? 1 : 2;
            int attack = PressureMap(pressureCase == 0 ? 50 : 1);
            string pressureOriginal = pressureRoot.PositionKey;
            Check(pressureRoot.CaptureAndAtariPressure(own, attack) > 0, "tactic#25 capture plus atari detected");
            Check(pressureRoot.PositionKey == pressureOriginal, "tactic#25 detection preserves root");
            if (pressureCase == 0)
                Check(pressureRoot.Generate(own) == PressureMap(50), "tactic#25 selects F6 response");
            else
            {
                int? selected = pressureRoot.Generate(own);
                Check(selected is int q && pressureRoot.HasTwoEyes(own, q), "tactic#25 preserves two eyes priority");
            }
            Check(new Board(19).CaptureAndAtariPressure(own, attack) == 0
                && new Board(9).CaptureAndAtariPressure(own, 40) == 0
                && pressureRoot.CaptureAndAtariPressure(own, -1) == 0, "tactic#25 nonmatches");
        }

        // 戦術#23：実戦1706191の二眼完成逃しを黒白八対称の全履歴で再現する。
        var twoEyeGame = new (int Color, int? Point)[] { (1, 29), (2, 50), (1, 33), (2, 32), (1, 21), (2, 24), (1, 22), (2, 23), (1, 38), (2, 57), (1, 42), (2, 51), (1, 41), (2, 40), (1, 47), (2, 34), (1, 43), (2, 52), (1, 44), (2, 35), (1, 56), (2, 65), (1, 58), (2, 48), (1, 49), (2, 67), (1, 59), (2, 68), (1, 60), (2, 39), (1, 14), (2, 64), (1, 15), (2, 16), (1, 61), (2, 53), (1, 42), (2, 69), (1, 33), (2, 36), (1, 43), (2, 55), (1, 70), (2, 79), (1, 4), (2, 37), (1, 46), (2, 28), (1, 10), (2, 30), (1, 20), (2, 45), (1, 31), (2, 41), (1, 0), (2, 6), (1, 2), (2, 19), (1, 3), (2, 7), (1, 72), (2, 73), (1, 75), (2, 66), (1, 76), (2, 25), (1, 77), (2, 5), (1, 13), (2, 54), (1, 11), (2, 18) };
        for (int eyeSwap = 0; eyeSwap < 2; eyeSwap++)
        for (int eyeSym = 0; eyeSym < 8; eyeSym++)
        {
            int EyeMap(int p)
            {
                int x = p % 9, y = p / 9;
                if (eyeSym >= 4) x = 8 - x;
                for (int i = 0; i < eyeSym % 4; i++) (x, y) = (y, 8 - x);
                return x + y * 9;
            }
            var twoEyeRoot = new Board(9);
            foreach (var move in twoEyeGame)
                Check(twoEyeRoot.Play(eyeSwap == 0 ? move.Color : 3 - move.Color,
                    move.Point is int q ? EyeMap(q) : null), "tactic#23 full history");
            int own = eyeSwap == 0 ? 1 : 2;
            string before = twoEyeRoot.PositionKey;
            Check(twoEyeRoot.FindTwoEyeCompletingMoves(own).Contains(EyeMap(9)), "tactic#23 completion candidate");
            Check(twoEyeRoot.PositionKey == before, "tactic#23 observation unchanged");
            Check(twoEyeRoot.Generate(own) == EyeMap(9), "tactic#23 selects necessary triangle");
            Check(twoEyeRoot.HasTwoEyes(own, EyeMap(11)), "tactic#23 target two eyes");
            Check(!twoEyeRoot.FindTwoEyeCompletingMoves(own).Contains(EyeMap(1)), "tactic#23 alive extension excluded");
        }

        // 戦術#22：一眼形成の働きと、別連・偽眼・適用盤サイズの反例。
        foreach (int eyeColor in new[] { 1, 2 })
        foreach (int eyeSize in new[] { 9, 19 })
        {
            var eyeBoard = new Board(eyeSize);
            Check(eyeBoard.Play(eyeColor, eyeSize) && eyeBoard.Play(eyeColor, eyeSize + 1), "tactic#22 corner fixture");
            string eyeBefore = eyeBoard.PositionKey;
            Check(eyeBoard.FormsSinglePointEye(eyeColor, 1) == (eyeSize == 9), "tactic#22 nine only");
            Check(eyeBoard.PositionKey == eyeBefore, "tactic#22 root unchanged");
            Check(!eyeBoard.FormsSinglePointEye(eyeColor, eyeSize), "tactic#22 occupied");
            Check(!eyeBoard.FormsSinglePointEye(eyeColor, -1), "tactic#22 invalid coordinate");
        }
        foreach (int eyeColor in new[] { 1, 2 })
        {
            var singleEyeSeparateFixture = new Board(9);
            foreach (var (x, y) in new[] { (3, 4), (4, 3), (4, 5) })
                Check(singleEyeSeparateFixture.Play(eyeColor, y * 9 + x), "tactic#22 separate fixture");
            Check(!singleEyeSeparateFixture.FormsSinglePointEye(eyeColor, 41), "tactic#22 separate groups excluded");
            var singleEyeFalseFixture = new Board(9);
            foreach (var (x, y) in new[] { (3, 4), (4, 3), (4, 2), (4, 1), (5, 1), (6, 1), (6, 2), (6, 3), (6, 4), (6, 5), (5, 5), (4, 5), (3, 5) })
                Check(singleEyeFalseFixture.Play(eyeColor, y * 9 + x), "tactic#22 connected fixture");
            Check(singleEyeFalseFixture.Play(3 - eyeColor, 30) && singleEyeFalseFixture.Play(3 - eyeColor, 32), "tactic#22 diagonal enemies");
            Check(!singleEyeFalseFixture.FormsSinglePointEye(eyeColor, 41), "tactic#22 two diagonal enemies excluded");
        }
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        {
            int target = 4 * size + 4;
            var exposedBoard = new Board(size);
            Check(exposedBoard.Play(3 - color, target + 1)
                && exposedBoard.Play(3 - color, target + size), "tactic#21 exposed fixture legal");
            string exposedOriginal = exposedBoard.PositionKey; int beforeMoves = exposedBoard.MoveCount;
            Check(exposedBoard.ShouldDeferExposedSingle(color, target) == (size == 9),
                "tactic#21 exposed single limited to nine");
            Check(exposedBoard.PositionKey == exposedOriginal && exposedBoard.MoveCount == beforeMoves,
                "tactic#21 observation preserves root");
            if (size == 9)
            {
                var deferred = Enumerable.Range(0, 81)
                    .Where(p => exposedBoard.ShouldDeferExposedSingle(color, p)).ToHashSet();
                int? selected = exposedBoard.Generate(color);
                Check(selected is int p && !deferred.Contains(p), "tactic#21 selects available alternative");
            }
            var captureBoard = new Board(size);
            Check(new[] { target + 2, target + 1 - size, target + 1 + size }
                .All(p => captureBoard.Play(color, p))
                && captureBoard.Play(3 - color, target + 1)
                && captureBoard.Play(3 - color, target + size), "tactic#21 capture fixture legal");
            Check(!captureBoard.ShouldDeferExposedSingle(color, target), "tactic#21 capture exception");
        }
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        {
            var splitBoard = new Board(size);
            int target = 3 * size + 4;
            Check(splitBoard.Play(color, 3 * size + 2) && splitBoard.Play(3 - color, 4 * size + 3),
                "tactic#17 one-space fixture legal");
            string key = splitBoard.PositionKey; int before = splitBoard.MoveCount;
            Check(splitBoard.ShouldDeferOneSpaceSplit(color, target) == (size == 9),
                "tactic#17 one-space deferral limited to nine");
            Check(splitBoard.PositionKey == key && splitBoard.MoveCount == before, "tactic#17 root unchanged");
            if (size == 9)
            {
                var deferred = Enumerable.Range(0, size * size)
                    .Where(p => splitBoard.ShouldDeferOneSpaceSplit(color, p)).ToHashSet();
                int? selected = splitBoard.Generate(color);
                Check(selected is int p && !deferred.Contains(p), "tactic#17 selects available alternative");
            }
        }
        // 空き三角は通常候補を後回しにし、観測は盤面を変更しない。
        foreach (int size in new[] { 5, 9, 19 })
        foreach (int color in new[] { 1, 2 })
        {
            var triangleBoard = new Board(size);
            Check(triangleBoard.Play(color, 1) && triangleBoard.Play(color, size), "tactic#16 candidate fixture legal");
            string before = triangleBoard.PositionKey; int moveCount = triangleBoard.MoveCount;
            Check(triangleBoard.ShouldDeferEmptyTriangle(color, 0), "tactic#16 triangle deferred on every size");
            Check(triangleBoard.PositionKey == before && triangleBoard.MoveCount == moveCount, "tactic#16 leaves root unchanged");
            var deferred = Enumerable.Range(0, size * size).Where(p => triangleBoard.ShouldDeferEmptyTriangle(color, p)).ToHashSet();
            int? selected = triangleBoard.Generate(color);
            Check(selected is int p && !deferred.Contains(p), "tactic#16 chooses available non-triangle");
            Check(triangleBoard.MoveCount == moveCount + 1, "tactic#16 generates exactly one move");
            int target = 2 * size + 2;
            var capture = new Board(size);
            Check(capture.Play(3 - color, target - 1)
                && new[] { target + 1, target + size, target - 2, target - 1 - size, target - 1 + size }.All(p => capture.Play(color, p)), "tactic#16 capture fixture legal");
            Check(!capture.ShouldDeferEmptyTriangle(color, target), "tactic#16 capture allowed");
        }
        // 団子の形でも捕獲に必要なら後回しにせず、19路には新順位を使わない。
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        {
            int target = 4 * size + 4;
            var plain = new Board(size);
            Check(new[] { target + 1, target + size, target + size + 1 }.All(p => plain.Play(color, p)), "tactic#20 plain fixture legal");
            string before = plain.PositionKey; int countBefore = plain.MoveCount;
            Check(plain.ShouldDeferDumpling(color, target) == (size == 9), "tactic#20 limited to nine lines");
            Check(plain.PositionKey == before && plain.MoveCount == countBefore, "tactic#20 observation leaves root unchanged");
            if (size != 9) continue;
            var capture = new Board(size);
            Check(capture.Play(3 - color, target - 1)
                && new[] { target + 1, target + size, target + size + 1, target - 2, target - 1 - size, target - 1 + size }.All(p => capture.Play(color, p)), "tactic#20 capture fixture legal");
            Check(!capture.ShouldDeferDumpling(color, target), "tactic#20 capture exception");
        }
        // 空盤だけの布石。後手の初着手やパス後へ拡張しない。
        foreach (int size in new[] { 9, 13, 19 })
        foreach (int color in new[] { 1, 2 })
        {
            var opening = new Board(size);
            Check(opening.Generate(color) == 3 * size + 2, "tactic#12 empty-board komoku C4");
            Check(opening.MoveCount == 1 && opening.At(2, 3) == color, "tactic#12 plays exactly one stone");
            Check(opening.TacticApplications.Count(a => a.Id == "komoku-opening") == 1, "tactic#12 application recorded");
            opening.Generate(3 - color);
            Check(opening.TacticApplications.Count(a => a.Id == "komoku-opening") == 1, "tactic#12 not repeated after opponent stone");
            var passed = new Board(size);
            passed.Play(3 - color, null);
            passed.Generate(color);
            Check(!passed.TacticApplications.Any(a => a.Id == "komoku-opening"), "tactic#12 not enabled after pass");
        }
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        foreach (bool swap in new[] { false, true })
        foreach (bool top in new[] { false, true })
        foreach (bool right in new[] { false, true })
        {
            int Map(int x, int y)
            {
                if (swap) (x, y) = (y, x);
                if (right) x = size - 1 - x;
                if (top) y = size - 1 - y;
                return y * size + x;
            }
            var plan = new Board(size);
            Check(plan.Play(color, Map(2, 3)), "tactic#13 komoku setup");
            var inside = plan.FindPlannedCornerPoints(color);
            Check(inside.Count == 11 && inside.Contains(Map(0, 0)), "tactic#13 eleven empty planned points across symmetries");
            Check(plan.FindPlannedCornerPoints(3 - color).Count == 0, "tactic#13 opponent does not own plan");
            Check(!plan.FindOpenCornerKomokuMoves().Any(p => p == Map(2, 3) || p == Map(3, 2)), "tactic#13 occupied corner excluded in both orientations");
            int? next = plan.Generate(color);
            Check(next is int n && !inside.Contains(n) && plan.TacticApplications.Any(a => a.Id == "komoku-corner-plan"),
                "tactic#13 next move takes another corner rather than painting inside");
            var invaded = new Board(size);
            invaded.Play(color, Map(2, 3));
            Check(invaded.Play(3 - color, Map(1, 2)), "tactic#13 invasion remains legal");
            Check(!invaded.FindPlannedCornerPoints(color).Contains(Map(0, 0)), "tactic#13 contested corner releases planned exclusion");
        }
        foreach (int size in new[] { 3, 5 })
        {
            var small = new Board(size);
            Check(small.FindOpenCornerKomokuMoves().Count == 0 && small.FindPlannedCornerPoints(1).Count == 0,
                "tactic#13 small research boards unchanged");
        }
        // 戦術#24：局所アタリの救出は空き角への展開より優先する。
        foreach (int cornerColor in new[] { 1, 2 })
        for (int cornerSym = 0; cornerSym < 8; cornerSym++)
        {
            int CornerMap(int p)
            {
                int x = p % 9, y = p / 9;
                if (cornerSym >= 4) x = 8 - x;
                for (int i = 0; i < cornerSym % 4; i++) (x, y) = (y, 8 - x);
                return x + y * 9;
            }
            var urgentCorner = new Board(9);
            Check(urgentCorner.Play(cornerColor, CornerMap(29)), "tactic#24 rescue root");
            foreach (int q in new[] { 28, 30, 20 })
                Check(urgentCorner.Play(3 - cornerColor, CornerMap(q)), "tactic#24 surround root");
            Check(urgentCorner.Generate(cornerColor) == CornerMap(38), "tactic#24 rescue before open corner");
            var contactedCorner = new Board(9);
            Check(contactedCorner.Play(cornerColor, CornerMap(29))
                && contactedCorner.Play(3 - cornerColor, CornerMap(20)), "tactic#24 contact root");
            var remainingCorners = contactedCorner.FindOpenCornerKomokuMoves().ToArray();
            Check(contactedCorner.Generate(cornerColor) is int q2 && remainingCorners.Contains(q2),
                "tactic#24 chooses unclaimed corner in each symmetry");
        }
        // 戦術#24：19路も離れた角へ展開する。接触・非接触と局所アタリを色/八対称で区別。
        foreach (int cornerSize in new[] { 9, 19 })
        foreach (int cornerSide in new[] { 1, 2 })
        for (int direction = 0; direction < 8; direction++)
        foreach (string kind in new[] { "contact", "quiet", "atari" })
        {
            int MapCorner(int x, int y)
            {
                if ((direction & 4) != 0) x = cornerSize - 1 - x;
                for (int i = 0; i < (direction & 3); i++) (x, y) = (cornerSize - 1 - y, x);
                return y * cornerSize + x;
            }
            var opening = new Board(cornerSize);
            int far = cornerSize - 4;
            int[] setup = kind == "atari"
                ? new[] { MapCorner(2, 3), MapCorner(1, 3), MapCorner(far, 3), MapCorner(3, 3), MapCorner(far, far), MapCorner(2, 4) }
                : new[] { MapCorner(2, 3), MapCorner(far, far), MapCorner(far, 3), kind == "contact" ? MapCorner(3, 3) : MapCorner(far - 1, far) };
            for (int i = 0; i < setup.Length; i++)
                Check(opening.Play(i % 2 == 0 ? cornerSide : 3 - cornerSide, setup[i]), "tactic#24 legal opening fixture");
            var available = opening.FindOpenCornerKomokuMoves().ToArray();
            int? choice = opening.Generate(cornerSide);
            Check(kind == "atari" ? choice == MapCorner(2, 2) : choice is int q && available.Contains(q),
                "tactic#24 unclaimed corner unless local atari needs rescue");
        }
        // 戦術#34：伸びの後回しを、捕獲・アタリ・連絡と区別する。
        foreach (int side in new[] { 1, 2 })
        for (int direction = 0; direction < 8; direction++)
        foreach (string kind in new[] { "ordinary", "open", "connect", "atari", "capture" })
        {
            int Map(int p)
            {
                int x = p % 9, y = p / 9;
                if ((direction & 4) != 0) x = 8 - x;
                for (int i = 0; i < (direction & 3); i++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            var extension = new Board(9);
            Check(extension.Play(side, Map(48)), "tactic#34 friendly root");
            foreach (int q in new[] { 56, 58, 66 })
            {
                if (kind == "open" && q == 66) continue;
                Check(extension.Play(kind == "connect" && q == 56 ? side : 3-side, Map(q)), "tactic#34 surrounding root");
            }
            if (kind is "atari" or "capture")
                foreach (int q in new[] { 47, 65 }) Check(extension.Play(side, Map(q)), "tactic#34 counterexample root");
            if (kind == "capture") Check(extension.Play(side, Map(55)), "tactic#34 capture root");
            string unchanged = extension.PositionKey;
            Check(extension.ShouldDeferEnclosedExtension(side, Map(57)) == (kind == "ordinary"),
                "tactic#34 keeps capture atari connection and open extension");
            Check(extension.PositionKey == unchanged, "tactic#34 detector does not play");
        }
        // 戦術#20：実戦1706524の黒39H6は同じ連を詰める団子。三四線だけで選ばない。
        int[] dumplingLineHistory = { 29,50,33,48,21,37,23,42,31,34,24,56,38,46,58,49,57,59,60,68,51,43,16,47,13,28,10,69,11,41,64,65,55,19,70,53,61,79 };
        for(int swap=0;swap<2;swap++)
        for(int symmetry=0;symmetry<8;symmetry++)
        {
            int MapDumplingLine(int p) { int x=p%9,y=p/9; if(symmetry>=4)x=8-x; for(int k=0;k<symmetry%4;k++)(x,y)=(8-y,x); return y*9+x; }
            int DumplingLineColor(int c)=>swap==0?c:3-c;
            var root=new Board(9);
            for(int i=0;i<dumplingLineHistory.Length;i++)
                Check(root.Play(DumplingLineColor(i%2+1),MapDumplingLine(dumplingLineHistory[i])),"dumpling-line actual history");
            int owner=DumplingLineColor(1);
            Check(root.ShouldDeferDumpling(owner,MapDumplingLine(52)),"actual H6 dumpling");
            var deferred=Enumerable.Range(0,81).Select(p=>root.ShouldDeferDumpling(owner,p)).ToArray();
            int? selected=root.Generate(owner);
            Check(selected is int p && !deferred[p],"non-dumpling before line preference");
        }
        // 戦術#35：敵三石へ単独で触れる手と、離した味方を使う接触を区別。
        foreach(int contactSize in new[]{9,19})
        for(int swap=0;swap<2;swap++)
        for(int symmetry=0;symmetry<8;symmetry++)
        {
            int ContactColor(int c)=>swap==0?c:3-c;
            int MapContact(int p){int x=p%9,y=p/9;if(symmetry>=4)x=contactSize-1-x;for(int k=0;k<symmetry%4;k++)(x,y)=(contactSize-1-y,x);return y*contactSize+x;}
            var root=new Board(contactSize);
            foreach(int p in new[]{58,67,76})Check(root.Play(ContactColor(2),MapContact(p)),"strong line setup");
            string before=root.PositionKey;
            Check(root.ShouldDeferExposedSingle(ContactColor(1),MapContact(49)),"unsupported strong line contact deferred");
            Check(!root.ShouldDeferExposedSingle(ContactColor(1),MapContact(40)),"stand-off not contact");
            Check(root.PositionKey==before&&root.MoveCount==3,"contact detection nonmutating");
            Check(root.Play(ContactColor(1),MapContact(40)),"stand-off support");
            Check(root.Play(ContactColor(2),MapContact(49)),"enemy approaches support");
            Check(!root.ShouldDeferExposedSingle(ContactColor(1),MapContact(48)),"diagonal support contact allowed");
            var oldTwo=new Board(contactSize);
            Check(oldTwo.Play(ContactColor(2),MapContact(58)),"old contact above");
            Check(oldTwo.Play(ContactColor(2),MapContact(48)),"old contact left");
            Check(oldTwo.ShouldDeferExposedSingle(ContactColor(1),MapContact(49))==(contactSize==9),"two-liberty rule stays nine-only");
            var shorter=new Board(contactSize);
            foreach(int p in new[]{58,67})Check(shorter.Play(ContactColor(2),MapContact(p)),"short line setup");
            Check(!shorter.ShouldDeferExposedSingle(ContactColor(1),MapContact(49)),"two enemy stones not three");
        }
        var contactOpening = new Board(9);
        contactOpening.Play(1, 29);
        contactOpening.Play(2, 20);
        contactOpening.Generate(1);
        Check(contactOpening.TacticApplications.Any(a => a.Id == "komoku-corner-plan"),
            "tactic#24 contact alone preserves open corner priority");
        var lateOpening = new Board(9);
        for (int i = 0; i < 8; i++) lateOpening.Play(1, null);
        lateOpening.Generate(1);
        Check(!lateOpening.TacticApplications.Any(a => a.Id == "komoku-corner-plan"),
            "tactic#13 eight-ply boundary disables corner opening priority");
        // 実戦1705815の一線塗りを、黒白・8方向で再現して通常候補を三四線へ向ける。
        foreach (int color in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        {
            int MapOpening(int x, int y)
            {
                if (mirror) x = 8 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            var boundaryOpening = new Board(9);
            Check(boundaryOpening.Play(color, MapOpening(2, 3)), "tactic#14 C4 setup");
            Check(boundaryOpening.Play(3 - color, MapOpening(5, 5)), "tactic#14 F6 setup");
            Check(boundaryOpening.Play(color, MapOpening(6, 3)), "tactic#14 G4 setup");
            Check(boundaryOpening.Play(3 - color, MapOpening(5, 3)), "tactic#14 F4 setup");
            int? selected = boundaryOpening.Generate(color);
            Check(selected.HasValue, "tactic#14 chooses actual move");
            int point = selected!.Value;
            int edge = Math.Min(Math.Min(point % 9, 8 - point % 9), Math.Min(point / 9, 8 - point / 9));
            Check(edge is 2 or 3, "tactic#14 ordinary opening uses third or fourth line");
            Check(boundaryOpening.TacticApplications.Any(t => t.Id is "opening-lines" or "komoku-corner-plan"), "tactic#14 actual application recorded");
        }
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        {
            int MapRescue(int x, int y)
            {
                if (mirror) x = size - 1 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (size - 1 - y, x);
                return y * size + x;
            }
            Board SingleRescueSetup()
            {
                var b = new Board(size);
                b.Play(color, MapRescue(6, 3));
                foreach (var q in new[] { (5, 3), (6, 2), (6, 4) })
                    b.Play(3 - color, MapRescue(q.Item1, q.Item2));
                return b;
            }
            int rescuePoint = MapRescue(7, 3);
            var single = SingleRescueSetup();
            string position = single.PositionKey;
            Check(single.FindSafeSingleStoneRescueMoves(color).Contains(rescuePoint), "tactic#15 finds safe three-liberty single rescue");
            Check(single.PositionKey == position && single.MoveCount == 4 && single.TacticApplications.Count == 0, "tactic#15 probing is nonmutating");
            Check(single.Generate(color) == rescuePoint && single.TacticApplications.Any(t => t.Id == "safe-single-rescue"),
                "tactic#15 actual generation rescues single before ordinary opening");
            var narrow = SingleRescueSetup();
            Check(narrow.Play(3 - color, MapRescue(7, 2)), "tactic#15 narrow setup");
            Check(!narrow.FindSafeSingleStoneRescueMoves(color).Contains(rescuePoint), "tactic#15 two liberties are insufficient");
            var other = SingleRescueSetup();
            Check(other.Play(color, MapRescue(0, 0)) && other.Play(3 - color, MapRescue(1, 0)), "tactic#15 other atari setup");
            Check(!other.FindSafeSingleStoneRescueMoves(color).Contains(rescuePoint), "tactic#15 rejects rescue leaving another friendly stone capturable");
        }
        Check(new Board(5).FindSafeSingleStoneRescueMoves(1).Count == 0, "tactic#15 excludes small research board");
        foreach (int color in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        {
            int MapMiddle(int p)
            {
                int x = p % 9, y = p / 9;
                if (mirror) x = 8 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            var middle = new Board(9);
            int[] sequence = { 29, 50, 33, 48, 21, 38, 22, 28, 20, 42, 23, 34, 24, 67, 30, 37, 31, 25 };
            for (int i = 0; i < sequence.Length; i++)
                Check(middle.Play(i % 2 == 0 ? color : 3 - color, MapMiddle(sequence[i])), "tactic#14 actual midgame replay");
            int? selected = middle.Generate(color);
            Check(selected.HasValue, "tactic#14 midgame move exists");
            int point = selected!.Value;
            int edge = Math.Min(Math.Min(point % 9, 8 - point % 9), Math.Min(point / 9, 8 - point / 9));
            Check(edge is 2 or 3, "tactic#14 no automatic first-line return at eighteen plies");
            Check(middle.TacticApplications.Any(t => t.Id == "opening-lines"), "tactic#14 midgame preference recorded");
        }
        // 戦術#1: 完成形の認識だけを検証し、着手優先はまだ変更しない。
        foreach (int color in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        foreach (int location in Enumerable.Range(0, 3))
        {
            const int size = 9;
            int Point(int x, int y)
            {
                if (mirror) x = size - 1 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (size - 1 - y, x);
                return y * size + x;
            }
            // 隅6石、辺8石、中央11石。最小性の証明ではない。
            (int x, int y)[] shape = location switch
            {
                0 => new[] { (1, 0), (3, 0), (0, 1), (1, 1), (2, 1), (3, 1) },
                1 => new[] { (2, 0), (4, 0), (6, 0), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1) },
                _ => new[] { (3, 2), (4, 2), (5, 2), (2, 3), (4, 3), (6, 3), (2, 4), (3, 4), (4, 4), (5, 4), (6, 4) }
            };
            var alive = new Board(size);
            foreach (var (x, y) in shape) Check(alive.Play(color, Point(x, y)), "tactic#1 shape setup");
            int first = Point(shape[0].x, shape[0].y);
            Check(alive.HasTwoEyes(color, first), "tactic#1 same-chain two eyes across transformations");
            (int x, int y)[] eyePoints = location switch
            {
                0 => new[] { (0, 0), (2, 0) },
                1 => new[] { (3, 0), (5, 0) },
                _ => new[] { (3, 3), (5, 3) }
            };
            foreach (var (x, y) in eyePoints)
                Check(!alive.Play(3 - color, Point(x, y)), "tactic#1 opponent cannot enter either eye");
            Check(alive.Play(color, Point(eyePoints[0].x, eyePoints[0].y)), "tactic#1 legal own-eye fill");
            Check(!alive.HasTwoEyes(color, first), "tactic#1 one remaining eye is not two");
            var broken = new Board(size);
            foreach (var (x, y) in shape.Skip(1)) Check(broken.Play(color, Point(x, y)), "tactic#1 broken boundary setup");
            Check(!broken.HasTwoEyes(color, Point(shape[^1].x, shape[^1].y)), "tactic#1 open boundary rejected");
            string beforeCandidates = broken.PositionKey;
            int beforeCount = broken.MoveCount;
            var completing = broken.FindTwoEyeCompletingMoves(color);
            Check(completing.Contains(Point(shape[0].x, shape[0].y)), "tactic#1 find missing boundary move");
            Check(broken.PositionKey == beforeCandidates && broken.MoveCount == beforeCount && broken.Lessons.Reports.Count == 0,
                "tactic#1 probing leaves board and reports unchanged");
            foreach (int candidate in completing)
            {
                var proof = new Board(size);
                foreach (var (x, y) in shape.Skip(1)) proof.Play(color, Point(x, y));
                Check(proof.Play(color, candidate) && proof.HasTwoEyes(color, candidate), "tactic#1 every proposed move completes same-chain eyes");
            }
        }
        var twoPieces = new Board(9);
        foreach (int p in new[] { 2, 5, 9, 10, 11, 12, 13, 14 }) Check(twoPieces.Play(1, p), "two two-point pieces setup");
        Check(twoPieces.HasTwoEyes(1, 2), "two independent two-point regions are eyes without filling");
        Check(twoPieces.ProtectedTerritoryOwners()[0] == 1 && twoPieces.ProtectedTerritoryOwners()[3] == 1,
            "preserve multi-point pieces as land");
        Check(twoPieces.UnconfirmedStoneCount(1) == 0, "two pieces confirm chain without filling");
        Check(twoPieces.Play(2, 0) && !twoPieces.HasTwoEyes(1, 2), "invaded piece is unsettled, not automatically alive or dead");
        Check(twoPieces.UnconfirmedStoneCount(1) == 8 && twoPieces.UnconfirmedStoneCount(2) == 1, "invasion reports unknown, not dead");
        var onePiece = new Board(9);
        foreach (int p in new[] { 5, 9, 10, 11, 12, 13, 14 }) onePiece.Play(1, p);
        Check(!onePiece.HasTwoEyes(1, 5), "one connected multi-point region is not two eyes");
        var cornerTerritory = new Board(9);
        foreach (int point in new[] { 1, 3, 9, 10, 11, 12 }) cornerTerritory.Play(1, point);
        var territoryOwners = cornerTerritory.ProtectedTerritoryOwners();
        Check(territoryOwners[0] == 1 && territoryOwners[2] == 1, "pancake protects corner eye pieces");
        Check(territoryOwners[80] == 0, "pancake does not claim four-edge outside as protected land");
        Check(cornerTerritory.Play(2, 80), "opponent can invade outside of alive corner");
        var separateEyes = new Board(9);
        foreach (int p in new[] { 1, 9, 10, 7, 17, 16 }) Check(separateEyes.Play(1, p), "tactic#1 separate groups setup");
        Check(!separateEyes.HasTwoEyes(1, 1) && !separateEyes.HasTwoEyes(1, 7), "tactic#1 never sum eyes of separate chains");
        Check(!separateEyes.HasTwoEyes(1, -1) && !separateEyes.HasTwoEyes(1, 81) && !separateEyes.HasTwoEyes(2, 1), "tactic#1 invalid or wrong stone rejected");
        Check(new Board(9).TacticApplications.Count == 0, "tactic log starts empty");
        Check(new Board(9).FindTwoEyeCompletingMoves(1).Count == 0, "tactic#1 no one-move two eyes on empty board");
        var finishedTactic = new Board(3);
        for (int ply = 0; ply < Board.MoveLimit; ply++) finishedTactic.Play(ply % 2 + 1, null);
        Check(finishedTactic.FindTwoEyeCompletingMoves(1).Count == 0, "tactic#1 no candidate after move limit");
        var board = new Board(3);
        Check(board.Play(2, 0), "place white");
        Check(board.Play(1, 1) && board.Play(1, 3) && board.At(0, 0) == 0, "capture");
        Check(!board.Play(2, 0), "suicide rejected");
        Check(!board.Play(2, 1) && board.At(1, 0) == 1, "occupied rejected without mutation");
        Check(board.Play(2, null), "pass");
        var ko = new Board(5);
        foreach (int p in new[] { 11, 13, 7 }) Check(ko.Play(1, p), "ko black setup");
        foreach (int p in new[] { 12, 16, 18, 22 }) Check(ko.Play(2, p), "ko white setup");
        Check(ko.Play(1, 17) && ko.At(2, 2) == 0, "ko capture");
        Check(ko.StonesLostToImmediateReply(1, 17) == 0, "illegal ko recapture not treated as losing capture");
        Check(!ko.Play(2, 12), "immediate ko rejected");
        Check(ko.Play(2, null) && ko.Play(1, null) && ko.Play(2, 12), "ko after passes");
        var full = new Board(2);
        Check(full.Play(1, 0) && full.Play(1, 1) && full.Play(1, 2), "fill board");
        Check(full.Generate(1) is null, "no legal move passes");
        string Exchange(string commands)
        {
            var output = new StringWriter();
            new GtpEngine().Run(new StringReader(commands), output);
            return output.ToString();
        }
        Check(Exchange("# comment\n1 protocol_version\nboardsize 5\nplay b D4\n2 genmove w\nquit\nname\n") == "=1 2\n\n=\n\n=\n\n=2 A1\n\n=\n\n", "GTP framing, ids, comments, generation, quit");
        Check(Exchange("boardsize 1\nplay x A1\nplay b I1\nunknown\n") == "? unacceptable size\n\n? invalid color\n\n? invalid vertex\n\n? unknown command\n\n", "GTP errors");
        Check(Exchange("boardsize 9\nplay b A1\nclear_board\ngenmove w\n").EndsWith("= C4\n\n"), "clear board resets komoku opening");
        Check(Exchange("boardsize 19\nplay b T19\nplay w T19\n").EndsWith("? illegal move\n\n"), "19x19 coordinates");
        var eyes = new Board(3);
        foreach (int p in new[] { 1, 2, 3, 5, 6, 7, 8 }) Check(eyes.Play(1, p), "two-eye setup");
        Check(eyes.Lessons.Reports.Count == 0, "lessons initially unlearned");
        Check(eyes.Generate(1) is null && eyes.CountStones().Black == 7, "pancake passes instead of filling own two-eye territory");
        Check(eyes.ProtectedTerritoryOwners()[0] == 1 && eyes.ProtectedTerritoryOwners()[4] == 1, "pancake protects both own eyes");
        Check(eyes.Generate(1) is null, "pass only when no legal move remains");
        var externalEye = new Board(3);
        foreach (int p in new[] { 1, 2, 3, 5, 6, 7, 8 }) externalEye.Play(1, p);
        Check(externalEye.Play(1, 0), "policy does not reject externally supplied legal move");
        var trap = new Board(3);
        Check(trap.Play(2, 1) && trap.Play(2, 4), "capture trap setup");
        trap.Generate(1);
        Check(trap.At(0, 0) == 0 && trap.Lessons.Reports.Any(r => r.Id == "immediate-capture"), "learn from simulated opponent capture");
        var cycle = new Board(5);
        foreach (int p in new[] { 11, 13, 7 }) Check(cycle.Play(1, p), "cycle black setup");
        foreach (int p in new[] { 12, 16, 18, 22 }) Check(cycle.Play(2, p), "cycle white setup");
        for (int p = 0; p < 12; p++)
            if (cycle.At(p % 5, p / 5) == 0) Check(cycle.Play(1, p), "cycle scan prefix setup");
        Check(cycle.Play(1, 17) && cycle.Play(2, null) && cycle.Play(1, null), "cycle capture and passes");
        Check(cycle.Generate(2) != 12 && cycle.Lessons.Reports.Any(r => r.Id == "repeat-position"), "learn and avoid repeated board after ko passes");
        foreach (int size in new[] { 3, 5, 9, 19 })
        {
            var game = new Board(size);
            var seen = new HashSet<string>();
            int passes = 0, moves = 0;
            int repeats = 0;
            while (!game.IsFinished)
            {
                int? move = game.Generate(moves % 2 + 1);
                passes = move is null ? passes + 1 : 0;
                if (move is not null)
                {
                    string state = string.Concat(Enumerable.Range(0, size * size).Select(p => game.At(p % size, p / size)));
                    if (!seen.Add(state)) repeats++;
                }
                moves++;
            }
            Check(passes == 2 || moves == Board.MoveLimit, $"{size}x{size} self-play ends within 400 plies");
            Check(game.MoveCount == moves, "generated moves and passes counted exactly once");
            Console.WriteLine($"Self-play {size}x{size}: {moves} moves, {repeats} repeats, {game.CountStones().Result}");
        }
        Check(Exchange("known_command cgos-genmove_analyze\n").Contains("= true"), "analysis command advertised");
        foreach (string side in new[] { "b", "w" })
        {
            string setup = "boardsize 9\nkomi 7.5\nplay b A1\nplay w J9\n";
            string[] normal = Exchange(setup + $"genmove {side}\nshowboard\n").Split("\n\n");
            string[] analyzed = Exchange(setup + $"cgos-genmove_analyze {side}\nshowboard\n").Split("\n\n");
            string[] lines = analyzed[4].Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            using var json = System.Text.Json.JsonDocument.Parse(lines.First(x => x.StartsWith('{')));
            var root = json.RootElement;
            var value = root.GetProperty("moves")[0];
            Check(lines.Last() == "play " + normal[4][2..], "analysis chooses same move as genmove");
            Check(normal[5] == analyzed[5], "analysis applies exactly one same move");
            int black = root.GetProperty("blackStones").GetInt32(), white = root.GetProperty("whiteStones").GetInt32();
            int own = side == "b" ? black : white;
            Check(value.GetProperty("score").GetInt32() == (side == "b" ? black - white : white - black), "analysis score uses mover perspective");
            Check(Math.Abs(value.GetProperty("winrate").GetDouble() - (double)own / (black + white)) < 1e-12, "analysis reports stone ratio without komi");
            Check(root.GetProperty("comment").GetString()!.Contains("勝つ確率ではありません"), "analysis explains ratio proxy");
        }
        string emptyAnalysis = Exchange("boardsize 9\n" + string.Concat(Enumerable.Repeat("play b pass\n", 400)) + "cgos-genmove_analyze w\n");
        Check(emptyAnalysis.Contains("\"winrate\":0.5") && emptyAnalysis.Contains("play pass"), "empty board analysis is finite neutral ratio at move limit");
        Check(Exchange("cgos-genmove_analyze invalid\n").StartsWith("? invalid color"), "analysis rejects invalid color");
        Check(Exchange("kfw-lessons\n").Contains("[]"), "GTP lesson reports");
        var empty = new Board(9);
        Check(empty.Score(7.5) == new AreaScore(0, 0, 81, 7.5), "empty board is neutral");
        Check(empty.Score(7.5).Result == "W+7.5", "white komi");
        var area = new Board(3);
        foreach (int p in new[] { 1, 3, 5, 7 }) Check(area.Play(1, p), "area setup");
        Check(area.Score(0) == new AreaScore(9, 0, 0, 0), "stones plus surrounded empties");
        Check(area.Score(9).Result == "0", "draw");
        var mixed = new Board(3);
        Check(mixed.Play(1, 0) && mixed.Play(2, 8), "neutral setup");
        Check(mixed.Score(0) == new AreaScore(1, 1, 7, 0), "mixed boundary neutral");
        Check(mixed.Play(1, null) && mixed.Play(2, null) && mixed.IsFinished, "two passes finish");
        Check(mixed.Play(1, 1) && !mixed.IsFinished, "dispute can resume play");
        Check(Exchange("boardsize 9\nkomi 7.5\nplay b pass\nplay w pass\nfinal_score\n").EndsWith("= W+7.5\n\n"), "territory rule counts komi");
        Check(Exchange("final_score\n").StartsWith("? game not finished"), "no final score before passes");
        Check(area.CountStones() == new StoneScore(4, 0), "count stones instead of territory");
        Check(board.CountStones() == new StoneScore(2, 0), "captured stones not counted");
        Check(new StoneScore(2, 3).Result == "W+1" && new StoneScore(3, 2).Result == "B+1" && new StoneScore(2, 2).Result == "0", "stone wins and draw");
        Check(board.BlackPrisoners == 1 && board.WhitePrisoners == 0, "capture increments prisoners");
        Check(area.ScoreTerritory(0).BlackTerritory == 5, "territory excludes stones");
        Check(new TerritoryScore(4, 3, 2, 1, 0.5).Result == "B+1.5", "territory prisoners and komi");
        Check(new Board(9).ProtectedTerritoryOwners().All(c => c == 0), "empty board not territory");
        Check(eyes.Play(1, 0), "policy permits external legal play");
        Check(Exchange("boardsize 9\nkomi 2.5\nclear_board\nplay b pass\nplay w pass\nfinal_score\n").EndsWith("= W+2.5\n\n"), "clear preserves komi");
        var mixedRegion = new Board(5);
        mixedRegion.Play(1, 0); mixedRegion.Play(2, 24);
        Check(mixedRegion.ProtectedTerritoryOwners().All(c => c == 0), "mixed boundary not own territory");
        var resetCapture = new Board(3);
        resetCapture.Play(2, 0); resetCapture.Play(1, 1); resetCapture.Play(1, 3);
        int capturedBeforeIllegal = resetCapture.BlackPrisoners;
        Check(!resetCapture.Play(2, 0) && resetCapture.BlackPrisoners == capturedBeforeIllegal, "suicide leaves prisoners unchanged");
        Check(new Board(3).BlackPrisoners == 0 && new Board(3).WhitePrisoners == 0, "new board resets prisoners");
        var limit = new Board(9);
        for (int ply = 0; ply < 398; ply++) Check(limit.Play(ply % 2 + 1, null), "count pass");
        Check(!limit.Play(1, -1) && limit.MoveCount == 398, "illegal move not counted");
        Check(limit.Play(1, 0) && limit.MoveCount == 399 && !limit.ReachedMoveLimit, "399th move accepted");
        Check(limit.Generate(2) is not null && limit.MoveCount == 400 && limit.IsFinished, "400th generated move finishes game");
        string finalPosition = limit.PositionKey;
        Check(limit.Generate(1) is null && !limit.Play(1, 2) && !limit.Play(1, null) && limit.MoveCount == 400 && limit.PositionKey == finalPosition, "400-ply board frozen");
        string atLimit = string.Concat(Enumerable.Repeat("play b pass\n", 399)) + "play b A1\nfinal_score\nplay w B1\ngenmove w\n";
        Check(Exchange(atLimit).EndsWith("= B+360\n\n? game finished at 400 moves\n\n= pass\n\n"), "GTP provisional territory at 400th position");
        Check(Exchange(atLimit + "clear_board\nplay b A1\n").EndsWith("=\n\n=\n\n"), "clear resets move limit");
        // SEED2026112321 / game0 の290手目直後。H9で1石取り、H8で11石失う。
        const string reversalMoves = "0,1,2,3,4,5,6,7,9,8,10,11,1,12,13,14,15,16,18,17,19,20,21,22,23,4,24,13,25,27,26,7,16,28,0,1,8,9,19,10,29,18,30,31,32,33,34,36,35,37,38,39,40,41,42,43,33,44,45,46,47,48,49,50,51,52,54,53,55,56,29,21,38,30,57,47,58,29,59,60,41,61,63,62,64,65,66,67,68,69,70,71,73,74,75,72,54,45,55,63,76,64,77,54,78,79,7,0,17,2,50,67,6,7,15,8,16,23,17,24,7,25,26,32,33,34,40,35,41,8,6,7,16,15,42,6,49,17,50,51,57,58,68,59,40,33,41,42,75,49,76,50,77,40,66,78,66,57,76,68,75,77,75,66,-,76,-,16,-,19,-,26,-,38,-,41,-,55,-,70,-,73,-,75,80,0,1,2,3,4,5,6,7,9,8,10,11,1,12,13,14,15,16,18,17,19,20,21,22,23,4,24,13,25,27,26,7,16,28,0,1,8,9,19,10,29,18,30,31,32,33,34,36,35,37,38,39,40,41,42,43,33,44,45,46,47,48,49,50,51,52,54,53,55,56,29,21,38,30,57,47,58,29,59,60,41,61,63,62,64,65,66,67,68,69,70,71,73,74,75,72,54,45,55,63,76,64,77,54,78";
        Board ReplayReversal()
        {
            var replay = new Board(9);
            int ply = 0;
            foreach (string vertex in reversalMoves.Split(','))
                Check(replay.Play(ply++ % 2 + 1, vertex == "-" ? null : int.Parse(vertex)), "capture reversal replay");
            return replay;
        }
        var original = ReplayReversal();
        int originalWhite = original.CountStones().White;
        Check(original.Play(1, 79) && original.CountStones().White == originalWhite - 1, "reproduced one-stone capture");
        Check(original.StonesLostToImmediateReply(1, 79) == 11, "predict exact recapture loss");
        int beforeRecapture = original.CountStones().Black;
        Check(original.Play(2, 70) && original.CountStones().Black == beforeRecapture - 11, "reproduced eleven-stone loss");
        var avoided = ReplayReversal();
        Check(avoided.Generate(1) != 79, "pancake policy avoids reproduced losing capture");
        var lastPlyCapture = ReplayReversal();
        while (lastPlyCapture.MoveCount < 399) lastPlyCapture.Play(1, null);
        Check(lastPlyCapture.Play(1, 79) && lastPlyCapture.StonesLostToImmediateReply(1, 79) == 0, "400th capture has no 401st reply");
        Check(board.StonesLostToImmediateReply(1, 3) == 0, "safe capture not treated as losing capture");
        // 戦術#4: SEED2026112499 game0、従来101手目にパスした局面。
        Board SnapbackPosition()
        {
            var b = new Board(9); int ply = 0;
            foreach (string v in "0,1,2,3,4,5,6,7,9,8,10,11,1,12,13,14,15,16,18,17,19,20,21,22,23,4,24,13,25,27,26,7,16,28,0,1,8,9,19,10,29,18,30,31,32,33,34,36,35,37,38,39,40,41,42,43,33,44,45,46,47,48,49,50,51,52,54,53,55,56,29,21,38,30,57,47,58,59,60,61,63,62,64,65,66,67,68,50,41,69,59,70,73,71,74,76,75,78,77,-".Split(','))
                Check(b.Play(ply++ % 2 + 1, v == "-" ? null : int.Parse(v)), "snapback replay");
            return b;
        }
        var snapback = SnapbackPosition();
        int? continued = snapback.Generate(1);
        Check(continued == 79, "tactic#4 continues with safe snapback instead of pass");
        Check(snapback.TacticApplications.Count == 1 && snapback.TacticApplications[0] == new TacticApplication("resolve-before-pass", 101, 1, 79),
            "tactic#4 records actual application with move and point");
        int blackBefore = snapback.CountStones().Black;
        Check(snapback.Play(2, 80) && snapback.CountStones().Black == blackBefore - 1, "snapback sacrifices one stone");
        int whiteBefore = snapback.CountStones().White;
        Check(snapback.Generate(1) == 79 && snapback.CountStones().White == whiteBefore - 11, "snapback generator actually recaptures eleven stones");
        Check(snapback.StonesLostToImmediateReply(1, 79) == 0, "snapback recapture is not immediately lost");
        var ignoredSnapback = SnapbackPosition();
        Check(ignoredSnapback.Generate(1) == 79 && ignoredSnapback.Play(2, null), "snapback opponent declines capture");
        int ignoredWhite = ignoredSnapback.CountStones().White;
        Check(ignoredSnapback.Generate(1) == 80 && ignoredSnapback.CountStones().White < ignoredWhite, "snapback still captures when opponent passes");
        var repeatedSnapback = SnapbackPosition();
        Check(repeatedSnapback.Play(1, 79) && !repeatedSnapback.HasSafeSnapback(1, 79, new HashSet<string> { snapback.PositionKey }), "snapback respects repeated-position policy");
        var noTimeSnapback = SnapbackPosition();
        while (noTimeSnapback.MoveCount < 398) noTimeSnapback.Play(2, null);
        Check(noTimeSnapback.Play(1, 79) && !noTimeSnapback.HasSafeSnapback(1, 79, new HashSet<string>()), "no nonexistent recapture beyond 400 plies");
        var loneSacrifice = new Board(3);
        Check(loneSacrifice.Play(1, 0) && loneSacrifice.Play(2, 1), "legal losing sacrifice setup");
        Check(!loneSacrifice.HasSafeSnapback(1, 0, new HashSet<string>()), "ordinary losing sacrifice is not snapback");
        // 第二の観測例: SEED2026112550 game10、87手目。黒白を反転しても検証する。
        foreach (bool swap in new[] { false, true })
        {
            var secondSnap = new Board(9); int ply = 0;
            foreach (string v in "0,63,1,25,2,80,3,12,4,68,5,26,6,27,7,19,8,70,9,59,10,39,11,72,13,40,14,33,15,76,16,44,17,35,18,67,20,51,21,36,22,62,23,78,24,60,28,42,29,75,30,73,31,48,32,53,37,74,38,56,41,69,43,55,45,65,27,77,46,49,47,58,50,34,54,61,-,71,-,79,-,52,-,43,-,64".Split(','))
            {
                int c = ply++ % 2 + 1;
                Check(secondSnap.Play(swap ? 3 - c : c, v == "-" ? null : int.Parse(v)), "second snapback replay");
            }
            int color = swap ? 2 : 1;
            Check(secondSnap.Generate(color) == 57, "second snapback selected for both colors");
            int ownBefore = color == 1 ? secondSnap.CountStones().Black : secondSnap.CountStones().White;
            Check(secondSnap.Play(3 - color, 66), "second snapback legal opponent capture");
            int ownAfter = color == 1 ? secondSnap.CountStones().Black : secondSnap.CountStones().White;
            Check(ownBefore - ownAfter == 1, "second snapback loses exactly one");
            int enemyBefore = color == 1 ? secondSnap.CountStones().White : secondSnap.CountStones().Black;
            Check(secondSnap.Generate(color) == 57, "second snapback actual generator recaptures");
            int enemyAfter = color == 1 ? secondSnap.CountStones().White : secondSnap.CountStones().Black;
            Check(enemyBefore - enemyAfter == 40, "second snapback captures forty after one-stone sacrifice");
            Check(secondSnap.TacticApplications.Count == 1 && secondSnap.TacticApplications[0].Color == color,
                "second snapback log belongs to actual color");
        }
        // 戦術#3研究用の狭い侵入: 3点の隅領域と、別の2つの眼を持つ壁。
        foreach (int invader in new[] { 0, 1, 2 })
        {
            var pocket = new Board(9);
            foreach (int p in new[] { 3, 5, 7, 9, 10, 11, 12, 13, 14, 15, 16 })
                Check(pocket.Play(1, p), "invasion pocket living wall setup");
            Check(pocket.HasTwoEyes(1, 3), "invasion pocket wall has independent eyes");
            Check(pocket.Play(2, invader), "invasion pocket legal invasion");
            Check(pocket.ProtectedTerritoryOwners()[1 == invader ? 0 : 1] == 0, "invaded region reopens for defense");
            int? defense = pocket.Generate(1);
            Check(defense == (invader == 1 ? 0 : 1), "existing policy finds local defense in corner pocket");
            Check(pocket.HasTwoEyes(1, 3), "defense does not destroy wall life");
            if (invader == 1)
            {
                Check(!pocket.Play(2, 2), "intruder cannot extend into last liberty after boundary defense");
                Check(pocket.Play(2, null) && pocket.Generate(1) == 2, "continue to actual capture after opponent pass");
            }
            Check(pocket.CountStones().White == 0 && pocket.BlackPrisoners == 1, "pocket invasion resolved by actual capture, not dead label");
        }
        var unconfirmedWall = new Board(9);
        foreach (int p in new[] { 3, 9, 10, 11, 12 }) unconfirmedWall.Play(1, p);
        Check(unconfirmedWall.Play(2, 2) && !unconfirmedWall.HasTwoEyes(1, 3), "invasion defense uncertain wall setup");
        unconfirmedWall.Generate(1);
        Check(unconfirmedWall.TacticApplications.All(t => t.Id != "minimum-invasion-defense"), "uncertain wall does not trigger invasion priority");
        foreach (int color in new[] { 1, 2 })
        {
            var defense = new Board(9);
            foreach (int p in new[] { 3, 5, 7, 9, 10, 11, 12, 13, 14, 15, 16 }) defense.Play(color, p);
            Check(defense.Play(3 - color, 2) && defense.Generate(color) == 1, "one-move invasion capture for either color");
            Check(defense.TacticApplications.Any(t => t.Id == "minimum-invasion-defense" && t.Color == color && t.Point == 1), "invasion defense real application recorded");
        }
        // 戦術#5: 19路で1石取りを急ぎ、別の7石を失った実戦局面。
        var rescueBoard = new Board(19); int rescuePly = 0;
        foreach (string v in "18,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,38,37,39,40,1,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,57,56,58,59,60,61,23,4,62,42,63,64,65,66,67,68,69,70,71,72,73,74,76,75,77,78,79,80,81,82,83,26,7,84,45,85,64,86,29,10,87,48,88,89,90,91,92,93,95,94,96,97,98,99,100,101,102,103,104,105,106,123,107,108,109,110,111,112,114,113,115,116,117,118,119,120,121,122,124,125,126,127,128,129,130,131,133,132,134,135,136,137,138,139,140,141,142,143,144,124,145".Split(','))
            Check(rescueBoard.Play(rescuePly++ % 2 + 1, v == "-" ? null : int.Parse(v)), "atari rescue replay");
        int? rescue = rescueBoard.Generate(1);
        Check(rescue == 146 || rescue == 147, "save seven stones before unrelated one-stone capture");
        Check(rescueBoard.TacticApplications.Any(t => t.Id == "rescue-atari-chain"), "record actual atari rescue");
        if (rescue == 147) Check(rescueBoard.Play(2, 146), "recorded threat reply after capture rescue");
        foreach (int victim in new[] { 13, 32, 51, 70, 89, 108, 127 })
            Check(rescueBoard.At(victim % 19, victim / 19) == 1, "seven original stones survive immediate reply");
        var simpleRescue = new Board(3);
        foreach (int p in new[] { 0, 1 }) Check(simpleRescue.Play(1, p), "simple rescue black setup");
        foreach (int p in new[] { 2, 3 }) Check(simpleRescue.Play(2, p), "simple rescue white setup");
        Check(simpleRescue.Generate(1) == 4 && simpleRescue.TacticApplications.Any(t => t.Id == "rescue-atari-chain"), "safe extension rescues two stones");
        var noRescue = new Board(3);
        foreach (int p in new[] { 0, 1 }) Check(noRescue.Play(1, p), "no-rescue black setup");
        foreach (int p in new[] { 2, 3, 5, 7 }) Check(noRescue.Play(2, p), "no-rescue white setup");
        Check(!noRescue.Play(1, 4), "suicidal rescue is illegal");
        noRescue.Generate(1);
        Check(noRescue.TacticApplications.All(t => t.Id != "rescue-atari-chain"), "do not claim rescue when no safe candidate exists");
        // 戦術#6は候補抽出だけ。着手優先は比較後に判断する。
        foreach (int owner in new[] { 1, 2 })
        {
            var denial = new Board(19);
            for (int y = 3; y <= 15; y++)
                for (int x = 3; x <= 15; x++)
                {
                    if (x >= 5 && x <= 13 && y >= 5 && y <= 13 || y == 4 && (x == 4 || x == 14)) continue;
                    Check(denial.Play(owner, y * 19 + x), "denial wall setup");
                }
            int[] invasion = { 160, 161, 162, 163, 164, 179, 181, 183, 198, 199, 200 };
            for (int i = 0; i < invasion.Length; i++)
            {
                Check(denial.Play(3 - owner, invasion[i]), "denial invasion setup");
                if (i < 10) denial.Generate(owner);
            }
            string keyBefore = denial.PositionKey; int plyBefore = denial.MoveCount;
            Check(denial.FindTwoEyeDenialMoves(owner).SequenceEqual(new[] { 201 }), "unique safe enclosed eye denial");
            Check(denial.PositionKey == keyBefore && denial.MoveCount == plyBefore && denial.TacticApplications.Count == 0, "denial extraction has no game effects");
            Check(denial.Play(owner, 201), "denial move legal");
            Check(denial.FindTwoEyeCompletingMoves(3 - owner).Count == 0, "denial removes immediate completion");

            var doubleThreat = new Board(19);
            foreach (int p in new[] { 40,41,42,43,44,59,61,63,78,79,80,200,201,202,203,204,219,221,223,238,239,240 })
                doubleThreat.Play(3 - owner, p);
            Check(doubleThreat.FindTwoEyeCompletingMoves(3 - owner).Count == 2, "two independent completion threats");
            Check(doubleThreat.FindTwoEyeDenialMoves(owner).Count == 0, "no all-threat denial from blocking one point");
            var openThreat = new Board(19);
            foreach (int p in new[] { 40,41,42,43,44,59,61,63,78,79,80 }) openThreat.Play(3 - owner, p);
            Check(openThreat.FindTwoEyeDenialMoves(owner).Count == 0, "unbounded opponent shape excluded");
        }
        foreach (int owner in new[] { 1, 2 })
            foreach (int rotation in Enumerable.Range(0, 4))
                foreach (bool reflect in new[] { false, true })
                {
                    int TransformDenial(int p)
                    {
                        int x = p % 19, y = p / 19;
                        if (reflect) x = 18 - x;
                        for (int r = 0; r < rotation; r++) (x, y) = (18 - y, x);
                        return y * 19 + x;
                    }
                    var shape = new Board(19);
                    for (int y = 3; y <= 15; y++)
                        for (int x = 3; x <= 15; x++)
                        {
                            if (x >= 5 && x <= 13 && y >= 5 && y <= 13 || y == 4 && (x == 4 || x == 14)) continue;
                            shape.Play(owner, TransformDenial(y * 19 + x));
                        }
                    foreach (int p in new[] { 160,161,162,163,164,179,181,183,198,199,200 })
                        shape.Play(3 - owner, TransformDenial(p));
                    Check(shape.FindTwoEyeDenialMoves(owner).SequenceEqual(new[] { TransformDenial(201) }), "rotated reflected enclosed denial");
                    Check(shape.Play(3 - owner, TransformDenial(201)), "enemy two eyes completion legal");
                    Check(shape.HasTwoEyes(3 - owner, TransformDenial(160)), "enemy completed two eyes confirmed");
                    Check(shape.FindTwoEyeDenialMoves(owner).Count == 0, "do not deny already completed life");
                }
        var endedDenial = new Board(19);
        Check(endedDenial.Play(1, null) && endedDenial.Play(2, null), "denial two-pass terminal setup");
        Check(endedDenial.FindTwoEyeDenialMoves(1).Count == 0, "no denial after two passes");
        while (endedDenial.MoveCount < Board.MoveLimit) endedDenial.Play(1, null);
        Check(endedDenial.FindTwoEyeDenialMoves(2).Count == 0, "no denial after move limit");
        foreach (int owner in new[] { 1, 2 })
            foreach (int rotation in Enumerable.Range(0, 4))
            {
                int MapSplit(int p)
                {
                    int x = p % 9, y = p / 9;
                    for (int r = 0; r < rotation; r++) (x, y) = (8 - y, x);
                    return y * 9 + x;
                }
                Board SplitSetup(bool invaded, bool weak, bool open = false)
                {
                    var b = new Board(9);
                    int[] pocket = { 20,21,22,23,24,29,30,32,33 };
                    for (int y = 1; y <= 6; y++)
                        for (int x = 1; x <= 7; x++)
                        {
                            int p = y * 9 + x;
                            if (pocket.Contains(p) || p is 47 or 51 || weak && p is 13 or 14 || open && p == 19) continue;
                            b.Play(owner, MapSplit(p));
                        }
                    if (invaded) b.Play(3 - owner, MapSplit(20));
                    return b;
                }
                var invadedSplit = SplitSetup(true, false);
                string beforeSplit = invadedSplit.PositionKey; int movesBeforeSplit = invadedSplit.MoveCount;
                Check(invadedSplit.FindSmallInvasionAtariMoves(owner).SequenceEqual(new[] { MapSplit(21) }), "small invasion atari preserves five empty points");
                Check(invadedSplit.PositionKey == beforeSplit && invadedSplit.MoveCount == movesBeforeSplit && invadedSplit.TacticApplications.Count == 0, "split extraction is observational");
                Check(invadedSplit.Generate(owner) == MapSplit(21), "generate small invasion atari before unrelated play");
                Check(invadedSplit.TacticApplications.Any(t => t.Id == "split-small-invasion" && t.Point == MapSplit(21)), "record real split application");

                Check(SplitSetup(false, false).FindSmallInvasionAtariMoves(owner).Count == 0, "do not split uninvaded protected territory");
                Check(SplitSetup(true, true).FindSmallInvasionAtariMoves(owner).Count == 0, "weak boundary excluded from small invasion atari");
                Check(SplitSetup(true, false, true).FindSmallInvasionAtariMoves(owner).Count == 0, "open escape outside small region excluded");
                var alreadyAtari = SplitSetup(true, false);
                Check(alreadyAtari.Play(owner, MapSplit(21)), "make existing invasion atari");
                Check(alreadyAtari.FindSmallInvasionAtariMoves(owner).Count == 0, "already atari uses capture tactic instead");
                var doubleSplit = SplitSetup(false, false);
                Check(doubleSplit.Play(3 - owner, MapSplit(22)), "central invasion has two liberties");
                Check(doubleSplit.FindSmallInvasionAtariMoves(owner).SequenceEqual(new[] { MapSplit(21), MapSplit(23) }.Order()), "equal space gains use stable point order");

            }
        var endedSplit = new Board(9);
        endedSplit.Play(1, null); endedSplit.Play(2, null);
        Check(endedSplit.FindSmallInvasionAtariMoves(1).Count == 0, "no split after two passes");
        while (endedSplit.MoveCount < Board.MoveLimit) endedSplit.Play(1, null);
        Check(endedSplit.FindSmallInvasionAtariMoves(2).Count == 0, "no split after move limit");
        foreach (int owner in new[] { 1, 2 })
            foreach (int rotation in Enumerable.Range(0, 4))
                foreach (bool reflect in new[] { false, true })
                    foreach (string variant in new[] { "supported", "circular", "one-eye" })
                    {
                        int MapSupport(int p)
                        {
                            int x = p % 9, y = p / 9;
                            if (reflect) x = 8 - x;
                            for (int r = 0; r < rotation; r++) (x, y) = (8 - y, x);
                            return y * 9 + x;
                        }
                        var supported = new Board(9);
                        int[] holes = variant == "circular" ? new[] { 7,17 }
                            : variant == "one-eye" ? new[] { 7,17,26,60,70 } : new[] { 7,17,60,70 };
                        if (variant == "one-eye") supported.Play(3 - owner, MapSupport(17));
                        foreach (int p in Enumerable.Range(0, 81).Where(p => !holes.Contains(p) && p != 8))
                            supported.Play(owner, MapSupport(p));
                        supported.Play(owner, MapSupport(8));
                        string positionBeforeSupport = supported.PositionKey; int countBeforeSupport = supported.MoveCount;
                        Check(!supported.HasTwoEyes(owner, MapSupport(8)), "supported fixture is separate from same-chain eyes");
                        Check(supported.HasTwoEyesWithSupport(owner, MapSupport(8)) == (variant == "supported"), "supported eyes reject circular and one-eye dependencies");
                        Check(supported.SupportedOnlyStoneCount(owner) == (variant == "supported" ? 1 : 0), "supported-only count excludes base eyes");
                        Check(supported.PositionKey == positionBeforeSupport && supported.MoveCount == countBeforeSupport && supported.TacticApplications.Count == 0, "supported recognition is observational");
                    }
        var invalidSupported = new Board(9);
        Check(invalidSupported.SharedAdditionalStoneCount(1) == 0, "empty board has no additional shared stones");
        Check(!invalidSupported.HasTwoEyesWithSupport(1, -1), "supported eyes reject negative point");
        Check(!invalidSupported.HasTwoEyesWithSupport(2, 81), "supported eyes reject outside point");
        Check(!invalidSupported.HasTwoEyesWithSupport(1, 0), "supported eyes reject empty point");
        foreach (int owner in new[] { 1, 2 })
            foreach (int rotation in Enumerable.Range(0, 4))
                foreach (bool center in new[] { false, true })
                    foreach (bool weakAnchor in new[] { false, true })
                    {
                        int MapSupportedGroup(int p)
                        {
                            int x = p % 9, y = p / 9;
                            for (int r = 0; r < rotation; r++) (x, y) = (8 - y, x);
                            return y * 9 + x;
                        }
                        int[] group = center ? new[] { 31,39,40,41,49 } : new[] { 7,8,17 };
                        int[] shared = center ? new[] { 22,30,32,38,42,48,50,58 } : new[] { 6,16,26 };
                        int[] anchorEyes = center ? new[] { 10,70 } : new[] { 60,70 };
                        var supportedGroup = new Board(9);
                        foreach (int p in Enumerable.Range(0, 81))
                            if (!shared.Contains(p) && !(weakAnchor ? anchorEyes.Take(1) : anchorEyes).Contains(p))
                                supportedGroup.Play(owner, MapSupportedGroup(p));
                        Check(!supportedGroup.HasTwoEyes(owner, MapSupportedGroup(group[0])), "multiple-stone target has no same-chain eyes");
                        Check(supportedGroup.HasTwoEyes(owner, MapSupportedGroup(0)) == !weakAnchor, "support requires independent base eyes");
                        Check(supportedGroup.HasTwoEyesWithSupport(owner, MapSupportedGroup(group[0])) == !weakAnchor, "multiple-stone support rejects anchor missing one eye");
                        Check(supportedGroup.SupportedOnlyStoneCount(owner) == (weakAnchor ? 0 : group.Length), "supported-only count includes entire target group once");
                    }
        // 戦術#9: 合法に組み立てた小形、対称性、既出・手数境界を検証。
        foreach (int owner in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        foreach (bool closed in new[] { false, true })
        foreach (int pressure in new[] { 11, 7, 17 })
        {
            int MapEscape(int p)
            {
                int x = p % 5, y = p / 5;
                if (mirror) x = 4 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (4 - y, x);
                return y * 5 + x;
            }
            Board EscapeFixture()
            {
                var b = new Board(5); const string shape = "2002220020201202202002000";
                foreach (int p in Enumerable.Range(0, 25).Where(p => shape[p] == '2'))
                    if (!b.Play(3 - owner, MapEscape(p))) throw new InvalidOperationException("escape wall setup");
                if (closed)
                    foreach (int p in new[] { 2, 6 })
                        if (!b.Play(3 - owner, MapEscape(p))) throw new InvalidOperationException("escape blocked setup");
                if (!b.Play(owner, MapEscape(12)) || !b.Play(3 - owner, MapEscape(pressure)))
                    throw new InvalidOperationException("escape target setup");
                return b;
            }
            var escape = EscapeFixture(); string beforeEscape = escape.PositionKey; int beforeEscapeCount = escape.MoveCount;
            int[] expected = !closed && pressure != 7 ? new[] { MapEscape(7) } : Array.Empty<int>();
            Check(escape.FindPreAtariEscapeMoves(owner).SequenceEqual(expected), "pre-atari escape rejects blocked directions across symmetries");
            Check(escape.PositionKey == beforeEscape && escape.MoveCount == beforeEscapeCount && escape.Lessons.Reports.Count == 0 && escape.TacticApplications.Count == 0,
                "pre-atari escape observation does not change board or reports");
            if (!closed && pressure == 11)
            {
                var repeated = EscapeFixture(); var trial = EscapeFixture(); trial.Play(owner, MapEscape(7));
                var historyField = typeof(Board).GetField("history", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                ((HashSet<string>)historyField.GetValue(repeated)!).Add(trial.PositionKey);
                Check(repeated.FindPreAtariEscapeMoves(owner).Count == 0, "pre-atari escape rejects root history");
                var escapeKo = EscapeFixture();
                typeof(Board).GetField("previous", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(escapeKo, trial.PositionKey.Select(c => c - '0').ToArray());
                Check(escapeKo.FindPreAtariEscapeMoves(owner).Count == 0, "pre-atari escape rejects simple ko");
                foreach (int moveCount in new[] { 398, 399, 400 })
                {
                    var limited = EscapeFixture();
                    typeof(Board).GetField("<MoveCount>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(limited, moveCount);
                    Check(limited.FindPreAtariEscapeMoves(owner).Count == (moveCount == 398 ? 1 : 0), "pre-atari escape requires time for a real reply");
                }
                var passed = EscapeFixture(); passed.Play(owner, null); passed.Play(3 - owner, null);
                Check(passed.FindPreAtariEscapeMoves(owner).Count == 0, "pre-atari escape stops at two passes");
            }
        }
        foreach (int owner in new[] { 1, 2 })
        {
            var supportedEscape = new Board(9);
            foreach (int p in Enumerable.Range(0, 81).Where(p => !new[] { 7, 17, 60, 70 }.Contains(p) && p != 8)) supportedEscape.Play(owner, p);
            supportedEscape.Play(owner, 8);
            Check(supportedEscape.HasTwoEyesWithSupport(owner, 8) && supportedEscape.FindPreAtariEscapeMoves(owner).Count == 0, "pre-atari escape excludes supported living singleton");
            var protectedEscape = new Board(9);
            foreach (int p in Enumerable.Range(0, 81).Where(p => !new[] { 10, 11, 19, 20, 60, 70 }.Contains(p))) protectedEscape.Play(owner, p);
            Check(protectedEscape.ProtectedTerritoryOwners()[10] == owner && protectedEscape.FindPreAtariEscapeMoves(owner).Count == 0, "pre-atari escape leaves living wall and territory alone");
        }        // 戦術#9の限定連絡: 支えの眼が欠けると、単なる逃走とは区別する。
        foreach (int owner in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        foreach (bool weakEye in new[] { false, true })
        {
            int MapConnection(int p)
            {
                int x = p % 9, y = p / 9;
                if (mirror) x = 8 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            Board ConnectionFixture()
            {
                var b = new Board(9);
                foreach (int p in new[] { 4,6,8,13,14,15,16,17,20,21,22 })
                    if (!b.Play(owner, MapConnection(p))) throw new InvalidOperationException("connection wall setup");
                if (weakEye && !b.Play(owner, MapConnection(5))) throw new InvalidOperationException("connection weak eye setup");
                if (!b.Play(owner, MapConnection(18)) || !b.Play(3 - owner, MapConnection(27)))
                    throw new InvalidOperationException("connection target setup");
                return b;
            }
            var connection = ConnectionFixture();
            string beforeConnection = connection.PositionKey; int countBeforeConnection = connection.MoveCount;
            Check(connection.HasTwoEyes(owner, MapConnection(20)) == !weakEye, "living connection needs independently confirmed anchor");
            Check(!connection.HasTwoEyesWithSupport(owner, MapConnection(18)), "living connection target starts unconfirmed");
            Check(connection.FindPreAtariEscapeMoves(owner).Contains(MapConnection(19)), "ordinary escape remains when anchor has one eye");
            Check(connection.FindPreAtariLivingConnectionMoves(owner).SequenceEqual(weakEye ? Array.Empty<int>() : new[] { MapConnection(19) }), "living connection excludes weak anchor across transformations");
            Check(connection.PositionKey == beforeConnection && connection.MoveCount == countBeforeConnection && connection.Lessons.Reports.Count == 0 && connection.TacticApplications.Count == 0,
                "living connection extraction is observational");
            var completedConnection = ConnectionFixture(); completedConnection.Play(owner, MapConnection(19));
            Check(completedConnection.HasTwoEyesWithSupport(owner, MapConnection(18)) == !weakEye, "living connection confirms target only with intact eyes");
            if (!weakEye)
            {
                var repeatedConnection = ConnectionFixture();
                var historyField = typeof(Board).GetField("history", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                ((HashSet<string>)historyField.GetValue(repeatedConnection)!).Add(completedConnection.PositionKey);
                Check(repeatedConnection.FindPreAtariLivingConnectionMoves(owner).Count == 0, "living connection keeps root history exclusion");
                foreach (int moveCount in new[] { 398, 399, 400 })
                {
                    var limitedConnection = ConnectionFixture();
                    typeof(Board).GetField("<MoveCount>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(limitedConnection, moveCount);
                    Check(limitedConnection.FindPreAtariLivingConnectionMoves(owner).Count == (moveCount == 398 ? 1 : 0), "living connection keeps reply within move limit");
                }
                var passedConnection = ConnectionFixture(); passedConnection.Play(owner, null); passedConnection.Play(3 - owner, null);
                Check(passedConnection.FindPreAtariLivingConnectionMoves(owner).Count == 0, "living connection stops after two passes");
            }
        }        // 戦術#10: 捕獲を一手遅らせる正例と逃走反例。
        foreach (int owner in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        foreach (bool open in new[] { false, true })
        {
            int MapDelay(int p)
            {
                int x = p % 9, y = p / 9;
                if (mirror) x = 8 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            Board DelayFixture()
            {
                var b = new Board(9); int[] pocket = { 22,23,31,38,39,40,48,49,57 };
                foreach (int p in Enumerable.Range(0, 81))
                    if (!pocket.Contains(p) && !new[] { 10,70,63,64,65 }.Contains(p)
                        && !(open && new[] { 66,74 }.Contains(p)) && !b.Play(owner, MapDelay(p)))
                        throw new InvalidOperationException("delay wall setup");
                foreach (int p in new[] { 63,64,22 })
                    if (!b.Play(3 - owner, MapDelay(p))) throw new InvalidOperationException("delay target setup");
                return b;
            }
            var delay = DelayFixture(); string beforeDelay = delay.PositionKey; int countBeforeDelay = delay.MoveCount;
            Check(delay.CanDeferSmallCapture(owner, MapDelay(65), MapDelay(31)) == !open, "deferred capture distinguishes escape space across transformations");
            Check(delay.PositionKey == beforeDelay && delay.MoveCount == countBeforeDelay && delay.BlackPrisoners == 0 && delay.WhitePrisoners == 0 && delay.Lessons.Reports.Count == 0 && delay.TacticApplications.Count == 0,
                "deferred capture reading is observational");
            if (!open)
            {
                foreach (var input in new[] { (c: 0, p: MapDelay(65), a: MapDelay(31)), (c: 3, p: MapDelay(65), a: MapDelay(31)),
                    (c: owner, p: -1, a: MapDelay(31)), (c: owner, p: 81, a: MapDelay(31)),
                    (c: owner, p: MapDelay(65), a: -1), (c: owner, p: MapDelay(65), a: 81),
                    (c: owner, p: MapDelay(65), a: MapDelay(65)), (c: owner, p: MapDelay(0), a: MapDelay(31)) })
                    Check(!DelayFixture().CanDeferSmallCapture(input.c, input.p, input.a), "deferred capture rejects invalid or unavailable input");
                foreach (int moveCount in new[] { 397,398,399,400 })
                {
                    var limitedDelay = DelayFixture();
                    typeof(Board).GetField("<MoveCount>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(limitedDelay, moveCount);
                    Check(limitedDelay.CanDeferSmallCapture(owner, MapDelay(65), MapDelay(31)) == (moveCount == 397), "deferred capture needs three real plies");
                }
                foreach (bool delayedPosition in new[] { false, true })
                {
                    var repeatedDelay = DelayFixture(); var completedDelay = DelayFixture();
                    if (delayedPosition) { completedDelay.Play(owner, MapDelay(31)); completedDelay.Play(3 - owner, null); }
                    completedDelay.Play(owner, MapDelay(65));
                    ((HashSet<string>)typeof(Board).GetField("history", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(repeatedDelay)!).Add(completedDelay.PositionKey);
                    Check(!repeatedDelay.CanDeferSmallCapture(owner, MapDelay(65), MapDelay(31)), "deferred capture rejects immediate and delayed root history");
                }
                var delayKo = DelayFixture(); var alternativeDelay = DelayFixture(); alternativeDelay.Play(owner, MapDelay(31));
                typeof(Board).GetField("previous", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(delayKo, alternativeDelay.PositionKey.Select(c => c - '0').ToArray());
                Check(!delayKo.CanDeferSmallCapture(owner, MapDelay(65), MapDelay(31)), "deferred capture rejects simple ko alternative");
                var passedDelay = DelayFixture(); passedDelay.Play(owner, null); passedDelay.Play(3 - owner, null);
                Check(!passedDelay.CanDeferSmallCapture(owner, MapDelay(65), MapDelay(31)), "deferred capture stops at two passes");
            }
        }
        foreach (int owner in new[] { 1, 2 })
        {
            var captureEscape = new Board(5);
            foreach (int p in new[] { 5,6 }) captureEscape.Play(owner, p);
            foreach (int p in new[] { 10,11,0,1 }) captureEscape.Play(3 - owner, p);
            Check(!captureEscape.CanDeferSmallCapture(owner, 2, 24), "deferred capture rejects escape by extension or adjacent capture");
            captureEscape.Play(owner, 24);
            int capturedBeforeEscape = owner == 1 ? captureEscape.WhitePrisoners : captureEscape.BlackPrisoners;
            Check(captureEscape.Play(3 - owner, 7), "adjacent capture escape is legal");
            Check((owner == 1 ? captureEscape.WhitePrisoners : captureEscape.BlackPrisoners) - capturedBeforeEscape == 2,
                "adjacent capture opens two liberties");
            Check(captureEscape.Play(owner, 2) && captureEscape.PositionKey[0] == (char)('0' + 3 - owner) && captureEscape.PositionKey[1] == (char)('0' + 3 - owner),
                "original target survives old capture point after adjacent capture");
        }
        // 戦術#11: 共有眼と一眼・依存崩壊を黒白・回転・鏡映で検証する。
        foreach (int owner in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        foreach (int fixture in Enumerable.Range(0, 6))
        {
            int size = fixture < 4 ? 9 : 3;
            int MapShared(int p)
            {
                int x = p % size, y = p / size;
                if (mirror) x = size - 1 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (size - 1 - y, x);
                return y * size + x;
            }
            var shared = new Board(size);
            int[] stones = size == 9
                ? new[] { 4,13,22,23,29,30,31,32,39,41,45,46,47,48,49,54,55,56,57,63,64,66,72,74 }
                : fixture == 4 ? new[] { 0,2,4,6,8 } : new[] { 2,4,6,8 };
            foreach (int p in stones) Check(shared.Play(owner, MapShared(p)), "shared eye fixture is legal");
            if (fixture is 1 or 3) Check(shared.Play(owner, MapShared(40)), "fill private eye fixture");
            if (fixture is 2 or 3) Check(shared.Play(owner, MapShared(65)), "join shared eye fixture");
            string before = shared.PositionKey; int beforeMoves = shared.MoveCount;
            var recognized = shared.FindSharedPointEyeStones(owner);
            int expected = fixture switch { 0 => 24, 1 or 2 => 25, 4 => 5, _ => 0 };
            Check(recognized.Count == expected, "shared eyes reject single eye and cascading dependencies");
            int additional = shared.SharedAdditionalStoneCount(owner);
            Check(additional == (fixture switch { 0 => 24, 1 => 25, 4 => 5, _ => 0 }), "shared additional excludes already confirmed stones");
            Check(additional + shared.SupportedOnlyStoneCount(owner) <= shared.UnconfirmedStoneCount(owner),
                "additional recognition partitions old unconfirmed without double counting");
            Check(recognized.SequenceEqual(recognized.Order()) && recognized.Distinct().Count() == recognized.Count,
                "shared recognition is unique and sorted");
            Check(shared.FindSharedPointEyeStones(3 - owner).Count == 0, "shared recognition respects color");
            Check(shared.PositionKey == before && shared.MoveCount == beforeMoves && shared.TacticApplications.Count == 0
                && shared.BlackPrisoners == 0 && shared.WhitePrisoners == 0, "shared recognition is observational");
            shared.Play(owner, null); shared.Play(3 - owner, null);
            Check(shared.FindSharedPointEyeStones(owner).SequenceEqual(recognized), "shared static recognition works after two passes");
        }
        foreach (int invalidColor in new[] { -1,0,3 })
        {
            bool rejected = false;
            try { new Board(9).FindSharedPointEyeStones(invalidColor); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "shared eyes reject invalid color");
        }
        Console.WriteLine($"PASS: {count} checks");
    }
}
