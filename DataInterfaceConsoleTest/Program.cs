using FiveDChessDataInterface;
using System;
using System.Threading;
using System.IO;

namespace DataInterfaceConsoleTest
{
    class Program
    {
        static void Main()
        {
            DataInterface di;
            while (!DataInterface.TryCreateAutomatically(out di, out int numberOfProcesses))
            {
                Thread.Sleep(1000);
                Console.WriteLine("Current number of processes: " + numberOfProcesses);
            }

            Console.WriteLine("Process found. Initializing...");
            di.Initialize();
            Console.WriteLine("Ready!");
            Console.WriteLine("This program will create files recording games longer than 5 moves when they are finished");
            Console.WriteLine("It will also play a sound specified by tick.wav when a player in a timed game has used 1/3 of their time since the start of their turn or the previous tick");

            DoDataDump(di);
        }

        private static void DoDataDump(DataInterface di)
        {
            var chessboardLocation = di.MemLocChessArrayPointer.GetValue();

            var ccForegroundDefault = Console.ForegroundColor;
            var ccBackgoundDefault = Console.BackgroundColor;

            int oldCnt = 0;
            string lastRun="";
            int firstT=-100; // Used to get turn indices right on T0
			var lastP = di.GetCurrentPlayersTurn();
			var lastTime=di.GetCurT();
			System.Media.SoundPlayer player = new System.Media.SoundPlayer("Tick.wav");
            while (true)
            {
				var thisP = di.GetCurrentPlayersTurn();
				var thisTime = di.GetCurT();
				if(thisP!=lastP){// Reset timers
				    lastTime=thisTime;
					Console.WriteLine($"{{{lastTime}}}");
					lastP=thisP;
				}
				if (lastTime!=0 && (thisTime-1)*3<=(lastTime-1)*2){//Should always tick if thisTime==1 or thisTime==0
					player.Play();
					lastTime=thisTime;
					//System.Media.SystemSounds.Exclamation.Play();
				}
				// Potential reasons to beep:
				// time remaining is a power of 2
				// 1 minute remaining (only beep once for this?)
				// half the time you started the turn with
				// When you use up your increment
				// when you're significantly behind your opponent
				
				//if(di.GetCurT()<600)System.Media.SystemSounds.Exclamation.Play();
				
                var cnt = di.GetChessBoardAmount();
                if (cnt != oldCnt)
                {
                    if (cnt==0 && oldCnt>5){
                        var filename = "5dpgn"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".txt";
                        File.WriteAllText(filename,"[Mode \"5D\"]"+lastRun);
                    }
                    lastRun="";
                    oldCnt = cnt;
                    var cbs = di.GetChessBoards();

                    //Console.Clear();
                    int lastCol = -1;
                    int turnNumber = 0;
                    /* Debug:
                    for (int i = 0; i < cbs.Count; i++){
                        var board = cbs[i];
                        var mem = board.cbm;
                        
                        Console.WriteLine($"board{mem.boardId} (L{board.cbm.timeline:+#;-#;0}T{board.cbm.turn + 1}): val04?{mem.val04} moveNumber?{mem.moveNumber} nextInTimeline?{mem.nextInTimelineBoardId} branchesFrom{mem.previousBoardId} {mem.val19} orig{mem.ttPieceOriginId} ");
                        Console.WriteLine($"({mem.moveSourceL}T{mem.moveSourceT+1}){(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>({mem.moveDestL}T{mem.moveDestT+1}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                    }*/
                    for (int i = 0; i < cbs.Count; i++)
                    {
                        var board = cbs[i];
                        var mem1 = board.cbm;
                        //var mem=mem1;
                        //Console.WriteLine($"\nboard{mem.boardId} (L{board.cbm.timeline:+#;-#;0}T{board.cbm.turn + 1}): parent?{mem.parentBoardId} nextInTimeline?{mem.nextInTimelineBoardId} branchesFrom{mem.previousBoardId} {mem.val19} orig{mem.ttPieceOriginId} ");
                        if (mem1.boardId!=i) Console.WriteLine("Warning: id does not match index");
                        if (mem1.previousBoardId==-1) continue;
                        var mem = cbs[mem1.previousBoardId].cbm;
                        board = cbs[mem1.previousBoardId];
                        //Console.WriteLine($"\nparent{mem.boardId} (L{mem.timeline:+#;-#;0}T{mem.turn + 1}): prev?{mem.previousBoardId} nextInTimeline?{mem.nextInTimelineBoardId} branchesFrom{mem.previousBoardId} {mem.val19} orig{mem.ttPieceOriginId} ");
                        if (!(mem.moveSourceL==0 && mem.moveSourceT==0 && mem.moveSourceX==0 && mem.moveSourceY==0
                            && mem.moveDestL==0 && mem.moveDestT==0 && mem.moveDestX==0 && mem.moveDestY==0)){
                            var movetype=0;//Physical
                            //work out if it's a branch or a hop
                            if (i+1<cbs.Count){
                                if (cbs[i+1].cbm.moveNumber==mem1.moveNumber){
                                    if (cbs[i+1].cbm.timeline==mem.moveDestL){
                                        movetype=1; //Hop
                                    }
                                    else{
                                        movetype=2; //Branch
                                    }
                                    i+=1;
                                }
                            }
                                
                                    
                            if (mem.isBlacksMove!=lastCol){
                                lastCol = mem.isBlacksMove;
                                char c = 'w';
                                if(lastCol!=0) c='b';
                                else{
                                    turnNumber+=1;
                                    if(turnNumber==1){
                                        firstT=mem.moveSourceT-1;
                                    }
                                }
                                //Console.Write($"\n{turnNumber}{c}.");
                                if(c=='w'){
                                    Console.Write($"\r\n{turnNumber}.");
                                    lastRun+=$"\r\n{turnNumber}.";
                                }
                                else{
									Console.Write($"/ ");
									lastRun+=$"/ ";
								}
                            }
                            if (movetype==0){
                                Console.Write($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}{(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                                lastRun+=($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}{(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                            }
                            else if (movetype==1){
                                Console.Write($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                                lastRun+=($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                            }
                            else{ //movetype==2
                                Console.Write($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                                lastRun+=($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                            }
                        }
                        //Console.WriteLine($"Board: id {mem.boardId} L{board.cbm.timeline:+#;-#;0}T{board.cbm.turn + 1}");
                        //Console.WriteLine($"move: ({mem.moveSourceL}T{mem.moveSourceT+1}){(char)(97+mem.moveSourceX)}{1+mem.moveSourceY} to ({mem.moveDestL}T{mem.moveDestT+1}){(char)(97+mem.moveDestX)}{1+mem.moveDestY}");

                    }
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        internal static void WriteConsoleColored(string text, ConsoleColor foreground, ConsoleColor background)
        {
            var fOld = Console.ForegroundColor;
            var bOld = Console.BackgroundColor;


            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;

            Console.Write(text);


            Console.ForegroundColor = fOld;
            Console.BackgroundColor = bOld;

        }
    }
}
