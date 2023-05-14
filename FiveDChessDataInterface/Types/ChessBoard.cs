using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FiveDChessDataInterface
{
    public class ChessBoard
    {
        public ChessPiece[] Pieces { get; set; }

        // 84 bytes remaining



        public int width;
        public int height;

        public ChessBoardMemory cbm;

        public ChessBoard(ChessBoardMemory mem, int width, int height)
        {
            this.width = width;
            this.height = height;
            this.cbm = mem;

            this.Pieces = new ChessPiece[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    var srcIndex = (x * 8 + y) * 2;
                    this.Pieces[x * height + y] = ChessPiece.ParseFromTwoByteNotation(mem.positionData[srcIndex], mem.positionData[srcIndex + 1]);
                }
            }
        }
        
        public string toFEN(string timeline, string turn){
            var pieces="";
            for (int y = height-1; y >= 0; y--)
            {
                if(y<height-1) pieces+="/";
                for (int x = 0; x < width; x++)
                {
                    var p = Pieces[x*height+y];
                    if(p.IsEmpty){
                        var i=0;
                        while(x<width && Pieces[x*height+y].IsEmpty){
                            i += 1;
                            x += 1;
                        }
                        pieces += $"{i}"+p.FENSymbol();
                    }
                    else{
                        pieces += p.FENSymbol();
                    }
                }
            }
            return $"\r\n[{pieces}:{timeline}:{turn}:{(cbm.isBlacksMove==0?'w':'b')}]";
        }

        public class ChessPiece
        {
            public PieceKind Kind { get; }
            public bool IsBlack { get; }
            public bool IsWhite { get => this.Kind != PieceKind.Empty && !this.IsBlack; }
            public bool IsEmpty { get => this.Kind == PieceKind.Empty; }

            public static ChessPiece ParseFromTwoByteNotation(int pieceByte, byte colorByte)
            {
                if (colorByte > 2)
                {
                    throw new InvalidDataException("Color data for this piece was bigger than 2! " + colorByte);
                }

                var isBlack = colorByte == 2;
                PieceKind kind;
                if(Enum.IsDefined(typeof(PieceKind), pieceByte)){
                    kind=(PieceKind)pieceByte;
                }
                else{
                    Console.WriteLine($"{pieceByte}");
                    kind=PieceKind.Unknown;
                }

                return new ChessPiece(kind, isBlack);
            }

            public override string ToString()
            {
                if (this.Kind == PieceKind.Empty)
                {
                    return string.Empty;
                }

                return $"[{(this.IsBlack ? "B" : "W")}]{this.Kind}";
            }
            
            // For 5D FEN
            public string FENSymbol()
            {   var p = this.Kind switch {
                    PieceKind.Pawn => "P*",
                    PieceKind.King => "K*",
                    PieceKind.Rook => "R*",
                    PieceKind.Brawn => "W*",
                    _ => this.Notation()
                };
                    
                if (this.IsBlack) return p.ToLower();
                else return p;
            }
            
            // For 5DPGN moves
            public string Notation()
            {
                return this.Kind switch
                {
                    PieceKind.Pawn => "",
                    PieceKind.Knight => "N",
                    PieceKind.Bishop => "B",
                    PieceKind.Rook => "R",
                    PieceKind.Queen => "Q",
                    PieceKind.King => "K",
                    PieceKind.Unicorn => "U",
                    PieceKind.Dragon => "D",
                    PieceKind.Princess => "S",
                    PieceKind.Brawn => "W",
                    PieceKind.RoyalQueen => "Y",
                    PieceKind.AlsoUnknown => "?",
                    PieceKind.Commoner => "C",
                    _ => "?"//((int)this.Kind).ToString()
                };
            }

            public ChessPiece(PieceKind kind, bool isBlack)
            {
                this.Kind = kind;
                this.IsBlack = isBlack;
            }

            public enum PieceKind : int
            {
                Unknown = -1,
                Empty = 0,
                Pawn,
                Knight,
                Bishop,
                Rook,
                Queen,
                King,
                Unicorn,
                Dragon,
                AlsoUnknown,
                Brawn,
                Princess,
                RoyalQueen,
                Commoner
            }
        }

        public override string ToString()
        {
            var nonempty = this.Pieces.Where(x => x.Kind != ChessPiece.PieceKind.Empty).ToList();
            return $"Id: {this.cbm.boardId}, T{this.cbm.turn + 1}L{this.cbm.timeline}, PieceCount: {nonempty.Count(x => x.IsWhite)}/{nonempty.Count(x => x.IsBlack)} ";
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct ChessBoardMemory
    {
        public const int structSize = 228;

        public int boardId;
        public int timeline;
        public int turn;
        public int isBlacksMove;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8 * 8 * 2)]
        public byte[] positionData;

        public int moveNumber; //-1 until a move is made from this board. After a move is made, it becomes the number of moves made before that one.
        public int val05; //probably isn't an int - values seen: 1 257, 513, 1009807616 
        public int moveSourceL;
        public int moveSourceT;
        public int moveSourceIsBlack;
        public int moveSourceY;
        public int moveSourceX;
        public int moveDestL;
        public int moveDestT;
        public int moveDestIsBlack;
        public int moveDestY;
        public int moveDestX;
        public int creatingMoveNumber; // moveNumber of the move that created this board
        public int nextInTimelineBoardId;// The id of the next board in the same timeline as this one
        public int previousBoardId; // the id of the board that was before this board, or this board branches off after
        public int val19;

        public int ttPieceOriginId; // the board id where this piece came from, or -1 if no timetravel happened

        // unconfirmed :

        public int ttMoveSourceY; // source timetravel move y (on the board where the piece disappeared) if source x and y are -1 then the piece is appearing on this board, coming from somewhere else
        public int ttMoveSourceX; // source timetravel move X
        public int ttMoveDestY;  // dest timetravel move y (on the board where the piece appeared) if dest x and y are -1 then the piece is disappearing on this board, going to somewhere else
        public int ttMoveDestX;

        // -----------

        public static ChessBoardMemory ParseFromByteArray(byte[] bytes)
        {
            if (Marshal.SizeOf<ChessBoardMemory>() != structSize)
                throw new InvalidOperationException("The size of this struct is not what it should be.");


            var gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var s = Marshal.PtrToStructure<ChessBoardMemory>(gch.AddrOfPinnedObject());
            gch.Free();

            return s;
        }
    }
}
