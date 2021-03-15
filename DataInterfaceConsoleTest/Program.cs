using FiveDChessDataInterface;
using System;
using System.Threading;


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

            DoDataDump(di);

            const int pollingIntervalMs = 10;

            var lastPlayer = -1;
            bool gameRunning = false;
            while (true)
            {
                while (di.GetChessBoardAmount() > 0)
                {
                    if (!gameRunning)
                    {
                        Console.WriteLine("Game has started!");
                        gameRunning = true;
                    }

                    var cp = di.GetCurrentPlayersTurn();
                    if (cp >= 0 && lastPlayer != cp) // if its any players turn, and the player changed
                    {
                        Console.WriteLine($"It's now {(cp == 0 ? "WHITE" : "BLACK")}'s turn!");
                        lastPlayer = cp;
                    }
                    Thread.Sleep(pollingIntervalMs);
                }

                if (gameRunning)
                {
                    Console.WriteLine("Game has ended!");
                    gameRunning = false;
                }

                Thread.Sleep(pollingIntervalMs);
            }
        }

        private static void DoDataDump(DataInterface di)
        {
            /*Console.WriteLine($"The pointer to the chessboards is located at: 0x{di.MemLocChessArrayPointer.Location.ToString("X16")}");
            Console.WriteLine($"The chessboard array size is located at: 0x{di.MemLocChessArraySize.Location.ToString("X16")}");
            Console.WriteLine($"The chessboard sizes width and height are located at 0x{di.MemLocChessBoardSizeWidth.Location.ToString("X16")} and 0x{di.MemLocChessBoardSizeHeight.Location.ToString("X16")}");

            Console.WriteLine($"The current turn is stored at: 0x{di.MemLocCurrentPlayersTurn.Location.ToString("X16")}");
            Console.WriteLine($"Currently it's {(di.GetCurrentPlayersTurn() == 0 ? "WHITE's" : "BLACK's")} turn!");*/


            var chessboardLocation = di.MemLocChessArrayPointer.GetValue();

            /*Console.WriteLine($"The chessboards are currently located at: 0x{chessboardLocation.ToString("X16")}");*/

            var ccForegroundDefault = Console.ForegroundColor;
            var ccBackgoundDefault = Console.BackgroundColor;

            int oldCnt = -1;
			int firstT=-100; // Used to get turn indices right on T0
            while (true)
            {
                var cnt = di.GetChessBoardAmount();
                if (cnt != oldCnt)
                {
                    oldCnt = cnt;


                    var cbs = di.GetChessBoards();

                    Console.Clear();
                    //Console.WriteLine("Chessboards: \n");
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
								if(c=='w') Console.Write($"\r\n{turnNumber}.");
								else Console.Write($"/ ");
							}
							if (movetype==0){
								Console.Write($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}{(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
							}
							else if (movetype==1){
								Console.Write($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
							}
							else{ //movetype==2
								Console.Write($"({mem.moveSourceL}T{mem.moveSourceT-firstT}){(char)(97+mem.moveSourceX)}{1+mem.moveSourceY}>>({mem.moveDestL}T{mem.moveDestT-firstT}){(char)(97+mem.moveDestX)}{1+mem.moveDestY} ");
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


            Console.WriteLine("Done!");
            Console.ReadLine();
            Environment.Exit(0);
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
