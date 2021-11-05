using FiveDChessDataInterface.Exceptions;
using FiveDChessDataInterface.MemoryHelpers;
using FiveDChessDataInterface.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FiveDChessDataInterface
{
    public class DataInterface
    {
        const string executableName = "5dchesswithmultiversetimetravel";
        public Process GameProcess { get; }
        public MemoryLocation<IntPtr> MemLocChessArrayPointer { get; private set; } // points to the chessboard array.
        public MemoryLocation<int> MemLocChessArraySize { get; private set; } // located right before the chessboard array pointer
        public MemoryLocation<int> MemLocChessBoardSizeWidth { get; private set; }
        public MemoryLocation<int> MemLocChessBoardSizeHeight { get; private set; }
        public MemoryLocation<int> MemLocCurrentPlayersTurn { get; private set; }
        //public MemoryLocation<int> MemLocInGameEndedScreen { get; private set; } // if 1 then the "you lost" / "you won" screen is shown
        public MemoryLocation<int> MemLocGameEndedWinner { get; private set; } // if 0xFFFF FFFF then the game is still running, 0 is a win for white, or unstarted, 1 a win for black or a draw
        public MemoryLocation<int> MemLocGameState { get; private set; } // if 0 then the game is running or unstarted, 1 means someone won, 2 is a draw
		public MemoryLocation<int> MemLocWhiteTime { get; private set; }
		public MemoryLocation<int> MemLocBlackTime { get; private set; }
		public MemoryLocation<int> MemLocWhiteIncrement { get; private set; }
		public MemoryLocation<int> MemLocBlackIncrement { get; private set; }
        public MemoryLocation<int> MemLocCosmeticTurnOffset { get; private set; }
        public MemoryLocation<int> MemLocEvenTimelines { get; private set; }
        
        public MemoryLocation<int> MemLocWhoAmI { get; private set; }
        public MemoryLocation<int> MemLocWhoAmI2 { get; private set; }
        public MemoryLocation<int> MemLocPerspective { get; private set; }
        
        public int GetWT() => this.MemLocWhiteTime.GetValue()+this.MemLocWhiteIncrement.GetValue();
        public int GetBT() => this.MemLocBlackTime.GetValue()+this.MemLocBlackIncrement.GetValue();
		public int GetCurT() => this.MemLocCurrentPlayersTurn.GetValue()==0?GetWT():GetBT();
        
        
        public void showPdata(){
            Console.WriteLine($"who1 : {MemLocWhoAmI.GetValue()}  |  who2  : {MemLocWhoAmI2.GetValue()}  |  persp : {MemLocPerspective.GetValue()}");
        }
        
        public int whichPlayerIsLocal(){
            //int v1 = MemLocWhoAmI.GetValue();
            //int v2 = MemLocWhoAmI2.GetValue();
            //return v1==v2?v1:2;
            return MemLocWhoAmI2.GetValue();
        }


        public IntPtr GetGameHandle() => this.GameProcess.Handle;
        public IntPtr GetEntryPoint() => this.GameProcess.MainModule.BaseAddress;

        public static bool TryCreateAutomatically(out DataInterface di)
        {
            var filteredProcesses = Process.GetProcessesByName(executableName);
            if (filteredProcesses.Length == 1)
            {
                di = new DataInterface(filteredProcesses[0]);
                return true;
            }

            di = null;
            return false;
        }

        public static bool TryCreateAutomatically(out DataInterface di, out int numberOfProcesses)
        {
            var filteredProcesses = Process.GetProcessesByName(executableName);
            numberOfProcesses = filteredProcesses.Length;

            if (filteredProcesses.Length == 1)
            {
                di = new DataInterface(filteredProcesses[0]);
                return true;
            }

            di = null;
            return false;
        }

        public static DataInterface CreateAutomatically()
        {
            if (TryCreateAutomatically(out var di, out int numberOfProcesses))
                return di;

            if (numberOfProcesses == 0)
                throw new AmbiguousMatchException("There was more than one 5d chess process running.");
            else
                throw new ProcessNotFoundException("The 5d chess process could not be found.");
        }

        public DataInterface(Process gameProcess)
        {
            this.GameProcess = gameProcess;
        }

        public void Initialize()
        {
            Thread.Sleep(250); // wait 250ms so that all read/write memory commands work fine
            CalculatePointers();
        }



        private void CalculatePointers()
        {
            var bytesToFind = new byte[] { 0x48, 0x8b, 0x0d, 0x90, 0x90, 0x90, 0x90, 0x49, 0x69 };

            var results = MemoryUtil.FindMemoryWithWildcards(GetGameHandle(), GetEntryPoint(), (uint)this.GameProcess.MainModule.ModuleMemorySize, bytesToFind);

            if (results.Count != 1)
            {
                throw new AmbiguousMatchException($"{results.Count} memory locations matched, which is not 1!");
            }

            var result = results.First();
            var resultAddress = result.Key;
            var resultBytes = result.Value;
			//var bytes = KernelMethods.ReadMemory(GetGameHandle(), resultAddress, 0x1000, out uint _);
			
            var chessboardPointerLocation = IntPtr.Add(resultAddress, BitConverter.ToInt32(resultBytes, 3) + 7);

            this.MemLocChessArrayPointer = new MemoryLocation<IntPtr>(GetGameHandle(), chessboardPointerLocation);
            this.MemLocChessArraySize = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, -8);
            this.MemLocChessBoardSizeWidth = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0xA8 + 0x4);
            this.MemLocChessBoardSizeHeight = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0xA8);
            this.MemLocCurrentPlayersTurn = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0x130);
            this.MemLocGameEndedWinner = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0xCC);
            this.MemLocGameState = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0xD0);
            this.MemLocWhiteTime = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0x1A8);
            this.MemLocBlackTime = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0x1AC);
            this.MemLocWhiteIncrement = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0x1B0);
            this.MemLocBlackIncrement = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, 0x1B4);
            this.MemLocCosmeticTurnOffset = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, -0x20);
            this.MemLocEvenTimelines = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, -0x34); // 0 if odd number of starting timelines, -1 if even
            
            this.MemLocWhoAmI = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, -0x610);
            this.MemLocWhoAmI2 = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, -0x44);
            this.MemLocPerspective = new MemoryLocation<int>(GetGameHandle(), chessboardPointerLocation, -0x40);
            //possibilities: -0x610, -0x44
            // -0x40 is the perspective
			
			
        }

        /// <summary>
        /// Gets all currently available chessboards. Chessboards are ordered by their id, indicating when they were created. (<see cref="ChessBoard.Id"/>).
        /// </summary>
        /// <returns>A list of <see cref="ChessBoard"/>, each representing a single chessboard.</returns>
        public List<ChessBoard> GetChessBoards()
        {
            var len = this.MemLocChessArraySize.GetValue();
            var bytesToRead = (uint)(len * ChessBoardMemory.structSize);
            var boardLoc = this.MemLocChessArrayPointer.GetValue();
            var bytes = KernelMethods.ReadMemory(GetGameHandle(), boardLoc, bytesToRead, out uint bytesRead);

            if (bytesToRead != bytesRead)
                throw new Exception("Not all bytes have been read!");

            var chunks = new List<byte[]>();
            for (int i = 0; i < len; i++)
            {
                var dest = new byte[ChessBoardMemory.structSize];
                Array.Copy(bytes, i * ChessBoardMemory.structSize, dest, 0, ChessBoardMemory.structSize);
                chunks.Add(dest);
            }

            var cbms = chunks.Select(x => ChessBoardMemory.ParseFromByteArray(x)).ToList();
            var chessboardSize = GetChessBoardSize();
            var cbs = cbms.Select(x => new ChessBoard(x, chessboardSize.Width, chessboardSize.Height)).ToList();
            return cbs;
        }

        public int GetChessBoardAmount() => this.MemLocChessArraySize.GetValue();

        /// <summary>
        /// Gets the current chessboard size.
        /// </summary>
        /// <returns>A <see cref="ChessBoardSize"/> object representing the size of all chessboards.</returns>
        public ChessBoardSize GetChessBoardSize() => new ChessBoardSize(this.MemLocChessBoardSizeWidth.GetValue(), this.MemLocChessBoardSizeHeight.GetValue());

        /// <summary>
        /// Gets the current player's turn.
        /// </summary>
        /// <returns>Returns 0 if it's WHITE's turn, and 1 if it's BLACK's turn.</returns>
        public int GetCurrentPlayersTurn() => this.MemLocCurrentPlayersTurn.GetValue();

        public bool IsGameRunning() => this.MemLocChessArrayPointer.GetValue() != IntPtr.Zero;

        public GameState GetCurrentGameState()
        {
            if (!IsGameRunning())
            {
                return GameState.NotStarted;
            }
            else
            {
                var whoWon = this.MemLocGameEndedWinner.GetValue();
                var gs = this.MemLocGameState.GetValue();

                if (gs == 0)
                {
                    if(whoWon!=-1){//
                        Console.WriteLine("Unexpected Data - gs is 0(running) but winning player '{whoWon}' is not -1");
                        //throw new UnexpectedChessDataException();
                    }
                    return GameState.Running;
                }
                if (gs==1){
                    if(whoWon==0){
                        return GameState.EndedWhiteWon;
                    }
                    if(whoWon==1){
                        return GameState.EndedBlackWon;
                    }
                    else{
                        Console.WriteLine("Unexpected Data - gs is 1(ended with checkmate) but winning player '{whoWon}' is not 0 or 1");
                        return GameState.Unknown;
                    }
                }
                if (gs==2){
                    return GameState.EndedDraw;
                }
                if(gs==3){
                    //Forfeit
                    if(whoWon==0){
                        return GameState.EndedWhiteWon;
                    }
                    if(whoWon==1){
                        return GameState.EndedBlackWon;
                    }
                    else{
                        Console.WriteLine("Unexpected Data - gs is 3(forfeit) but winning player '{whoWon}' is not 0 or 1");
                        return GameState.Unknown;
                    }
                }
                if(gs==5){
                    //Time
                    if(whoWon==0){
                        return GameState.EndedWhiteWon;
                    }
                    if(whoWon==1){
                        return GameState.EndedBlackWon;
                    }
                    else{
                        Console.WriteLine("Unexpected Data - gs is 5(timeout) but winning player '{whoWon}' is not 0 or 1");
                        return GameState.Unknown;
                    }
                }
                else{
                    Console.WriteLine("Unexpected Data - gs is not 0,1,2,3 or 5");
                    return GameState.Unknown;
                }
            }
        }
    }
}