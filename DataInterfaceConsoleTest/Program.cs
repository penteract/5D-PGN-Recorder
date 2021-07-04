using FiveDChessDataInterface;
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using FiveDChessDataInterface.Types;

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
            Console.WriteLine("It will also play a sound specified by Tick.wav when a player in a timed game has used 1/3 of their time since the start of their turn or the previous tick");

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
			var lastTime = di.GetCurT();
			System.Media.SoundPlayer player = new System.Media.SoundPlayer("Tick.wav");
            List<int> times = new List<int>();
            int oldTurn = 0;
            bool written = false;
            var oldState = GameState.NotStarted;
            var startDate = "";
            var startTime = "";
            var localPlayer = -1;
            while (true)
            {
				var thisP = di.GetCurrentPlayersTurn();
				var thisTime = di.GetCurT();
				if(thisP!=lastP){// Reset timers
				    lastTime=thisTime;
					lastP=thisP;
				}
				if (lastTime!=0 && (thisTime-1)*3<=(lastTime-1)*2){//Should always tick if thisTime==1 or thisTime==0
                    try{
                        player.Play();
                    }
                    catch {
                    }
                    lastTime=thisTime;
                    //System.Media.SystemSounds.Exclamation.Play();
				}
				// Potential reasons to beep:
				// time remaining is a power of 2
				// 1 minute remaining (only beep once for this?)
				// half the time you started the turn with
				// When you use up your increment
				// when you're significantly behind your opponent
				
				//di.showPdata();
                var cnt = di.GetChessBoardAmount();
                var turn = di.GetCurrentPlayersTurn();
                //turn != oldTurn is there to make sure we record times, but we shouldn't rely 
                // on noticing this change because we only poll twice per second
                // I think there's technically a case we miss where a player undoes a move
                // then submits, then the opponent makes a one-move action and submits.
                // there's only a problem if that all happens in half a second, so I'm not too worried, but if we find the memory location of the move number,
                //that would be better than tracking the turn.
                var state = di.GetCurrentGameState();
                if (cnt != oldCnt || turn != oldTurn || state!=oldState)
                {
                    if (cnt==0 && oldCnt>5 && !written){
                        var filename = "5dpgn"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".txt";
                        File.WriteAllText(filename,lastRun);
                        Console.WriteLine($"written to file '{filename}'");
                    }
                    if (oldCnt==0){
                        times = new List<int>();
                        written = false;
                        startDate = DateTime.Now.ToString("yyyy.MM.dd");
                        startTime = DateTime.Now.ToString("HH:mm:ss (zzz)");
                        localPlayer = di.whichPlayerIsLocal(); //0 white, 1 black, 2 both
                    }
                lastRun=$"[Mode \"5D\"]\r\n[Result \"{GameStateToResult(state)}\"]\r\n[Date \"{startDate}\"]\r\n[Time \"{startTime}\"]\r\n[Size \"{di.GetChessBoardSize().toString()}\"]";
                    if(localPlayer==1){
                        lastRun+="\r\n[White \"Opponent\"]";
                        //lastRun+="\r\n[Black \"tesseract\"]";
                    }
                    if(localPlayer==0){
                        //lastRun+="\r\n[White \"tesseract\"]";
                        lastRun+="\r\n[Black \"Opponent\"]";
                    }
                    oldCnt = cnt;
                    oldTurn = turn;
                    oldState = state;
                    var cbs = di.GetChessBoards();

                    //Console.Clear();
                    int lastCol = -1;
                    int turnNumber = 0;
                    for (int i = 0; i < cbs.Count; i++)
                    {
                        var board = cbs[i];
                        var mem1 = board.cbm;
                        if (mem1.boardId!=i) Console.WriteLine("Warning: id does not match index");
                        if (mem1.previousBoardId==-1) continue;
                        board = cbs[mem1.previousBoardId];
                        var mem = board.cbm;
                        if (mem.moveSourceL==0 && mem.moveSourceT==0 && mem.moveSourceX==0 && mem.moveSourceY==0
                            && mem.moveDestL==0 && mem.moveDestT==0 && mem.moveDestX==0 && mem.moveDestY==0) continue;
                          
                        var movetype=0;//Physical
                        //work out if it's a branch or a hop
                        if (i+1<cbs.Count){
                            if (cbs[i+1].cbm.creatingMoveNumber==mem1.creatingMoveNumber){
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
                            if (lastCol==1){
                                c = 'b';
                            }
                            else{
                                turnNumber+=1;
                                if(turnNumber==1){
                                    firstT=mem.moveSourceT-1;
                                    if (firstT==0)
                                        lastRun+=$"\r\n[Board \"Standard - Turn Zero\"]";
                                }
                            }
                            //Console.Write($"\n{turnNumber}{c}.");
                            if(c=='w'){
                                lastRun+=$"\r\n{turnNumber}.";
                            }
                            else{
                                lastRun+=$"/ ";
                            }
                            var n = turnNumber*2 + lastCol - 1;
                            if ( times.Count < n){
                                times.Add(c=='w'?di.GetWT():di.GetBT());
                            }
                            else if(times.Count == n){
                                times[n-1] = c=='w'?di.GetWT():di.GetBT();
                            }
                            if(times[0]!=0){
                                var t = times[n-1];
                                lastRun+=$"{{{t/60}:{t%60:00}}}";
                            }
                        }
                        if (movetype==0){
                            lastRun+=($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}{(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                        }
                        else if (movetype==1){
                            lastRun+=($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                        }
                        else{ //movetype==2
                            lastRun+=($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){board.Pieces[mem.moveSourceX*board.width+mem.moveSourceY].Notation()}{(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
                        }
                    }
                    Console.WriteLine(lastRun);
                    if (( state==GameState.EndedBlackWon
                          || state==GameState.EndedDraw
                          || state==GameState.EndedWhiteWon)
                        && !written
                        && cnt>5){
                        written = true;
                        var filename = "5dpgn"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".txt";
                        File.WriteAllText(filename,lastRun);
                        Console.WriteLine($"written to file '{filename}'");
                    }
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }
        
        private static string GameStateToResult(GameState s){
            return s switch {
                GameState.NotStarted => "NotStarted",
                GameState.Running => "*",
                GameState.EndedDraw => "1/2-1/2",
                GameState.EndedWhiteWon => "1-0",
                GameState.EndedBlackWon => "0-1",
                _ => "error"
            };
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
