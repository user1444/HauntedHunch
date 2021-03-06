﻿using System.Windows.Input;

namespace HauntedHunch
{
    public class GamePageViewModel
    {
        #region Variables and Properties

        public static double SquareWidth => 90;

        public double LabelWidth => SquareWidth / 3;

        const int nr = G.r; // Number of rows
        const int nc = G.c; // Number of columns

        public Square[,] table = new Square[nr + 1, nc + 1]; // Table is 7x5. Zero indexes are ignored for a better understanding of the coordinates, will always stay null.
        Square[,,] history = new Square[1000, nr + 1, nc + 1]; // Game history, for undo
        public Square selectedPiece; // Current moving piece
        Square interacter; // Interacting piece in moves where there is more than one piece involved
        public int turn = 0; // 4k+3 & 4k are white's turns, 4k+1 & 4k+2 are black's turns.
        bool placementStage = true;
        public bool gameEnded;
        public bool turnConstraintsEnabled = true;

        public ICommand DisableTurnConstraintsCommand => new RelayCommand(() => turnConstraintsEnabled = false);
        public ICommand UndoCommand => new RelayCommand(() =>
        {
            if (turn == 0) return;

            for (int i = 1; i <= nr; i++)
                for (int j = 1; j <= nc; j++)
                    history[turn, i, j] = null;

            do { turn--; }
            while (history[turn, 1, 1] == null);

            for (int i = 1; i <= nr; i++)
                for (int j = 1; j <= nc; j++)
                {
                    table[i, j].PseudoPiece = history[turn, i, j].PseudoPiece;
                    table[i, j].Piece = history[turn, i, j].Piece;
                }
        });

        #endregion

        #region Constructor

        public GamePageViewModel()
        {
            for (int i = 1; i <= nr; i++)
            {
                for (int j = 1; j <= nc; j++)
                {
                    if (i == 1 && j == 1) table[i, j] = new Square(i, j, new Guard(i, j, PlayerType.White));
                    else if (i == 1 && j == 2) table[i, j] = new Square(i, j, new Runner(i, j, PlayerType.White));
                    else if (i == 1 && j == 3) table[i, j] = new Square(i, j, new Ranger(i, j, PlayerType.White));
                    else if (i == 1 && j == 4) table[i, j] = new Square(i, j, new Jumper(i, j, PlayerType.White));
                    else if (i == 1 && j == 5) table[i, j] = new Square(i, j, new Lotus(i, j, PlayerType.White));
                    else if (i == 1 && j == 6) table[i, j] = new Square(i, j, new Guard(i, j, PlayerType.White));
                    else if (i == 2 && j == 1) table[i, j] = new Square(i, j, new Converter(i, j, PlayerType.White));
                    else if (i == 2 && j == 2) table[i, j] = new Square(i, j, new Courier(i, j, PlayerType.White));
                    else if (i == 2 && j == 3) table[i, j] = new Square(i, j, new Boomer(i, j, PlayerType.White));
                    else if (i == 2 && j == 4) table[i, j] = new Square(i, j, new InnKeeper(i, j, PlayerType.White));
                    else if (i == 2 && j == 5) table[i, j] = new Square(i, j, new Freezer(i, j, PlayerType.White));
                    else if (i == 2 && j == 6) table[i, j] = new Square(i, j, new MindController(i, j, PlayerType.White));
                    else if (i == 7 && j == 1) table[i, j] = new Square(i, j, new Guard(i, j, PlayerType.Black));
                    else if (i == 7 && j == 2) table[i, j] = new Square(i, j, new Runner(i, j, PlayerType.Black));
                    else if (i == 7 && j == 3) table[i, j] = new Square(i, j, new Ranger(i, j, PlayerType.Black));
                    else if (i == 7 && j == 4) table[i, j] = new Square(i, j, new Jumper(i, j, PlayerType.Black));
                    else if (i == 7 && j == 5) table[i, j] = new Square(i, j, new Lotus(i, j, PlayerType.Black));
                    else if (i == 7 && j == 6) table[i, j] = new Square(i, j, new Guard(i, j, PlayerType.Black));
                    else if (i == 6 && j == 1) table[i, j] = new Square(i, j, new Converter(i, j, PlayerType.Black));
                    else if (i == 6 && j == 2) table[i, j] = new Square(i, j, new Courier(i, j, PlayerType.Black));
                    else if (i == 6 && j == 3) table[i, j] = new Square(i, j, new Boomer(i, j, PlayerType.Black));
                    else if (i == 6 && j == 4) table[i, j] = new Square(i, j, new InnKeeper(i, j, PlayerType.Black));
                    else if (i == 6 && j == 5) table[i, j] = new Square(i, j, new Freezer(i, j, PlayerType.Black));
                    else if (i == 6 && j == 6) table[i, j] = new Square(i, j, new MindController(i, j, PlayerType.Black));
                    else table[i, j] = new Square(i, j);
                }
            } // Set up the initial board position.

            UpdateHistory();
        }

        #endregion

        #region LMDown

        /// <summary>
        /// Activated when clicked on a square.
        /// Does different things depending on the input and the state of the variables table, turn, cur and interactor.
        /// </summary>
        /// <param name="sen"> Square that is just clicked </param>
        public void Action(Square sen)
        {
            if (gameEnded) return;

            // If we are at an in-between move of an ability with interactor
            if (interacter != null)
            {
                if (sen.State == SquareState.AbilityWithInteracterable)
                {
                    selectedPiece.Piece.AbilityWithInteracterStageTwo(table, interacter, sen, ref turn);
                    UpdateHistory();
                    UpdatePits();
                }
                interacter = null;
                selectedPiece = null;
                return;
            }

            tunnel1: // Used when selecting a friendly piece to move while already had been selected a friendly piece to move

            if (selectedPiece == null) // If no piece is chosen yet
            {
                // If a valid piece is chosen, paint the possible moves for preview.
                if (sen.Piece != null && (!turnConstraintsEnabled ||
                    sen.Piece.Player == PlayerType.White && (turn % 4 == 0 || turn % 4 == 3) || sen.Piece.Player == PlayerType.Black && (turn % 4 == 1 || turn % 4 == 2)))
                {
                    sen.Piece.PossibleMoves(table, turn);
                    selectedPiece = sen;
                }
                // If a non-valid square is chosen, do nothing
                else
                {
                    // Explicitly kick other (cur == null) cases, don't merge 2 if statements.
                }
            }
            else // (cur != null)
            {
                // Ability Uno
                if (sen.State == SquareState.AbilityUnoable)
                {
                    selectedPiece.Piece.AbilityUno(table, ref turn);
                    UpdateHistory();

                    UpdatePits();
                    selectedPiece = null;
                }

                // Ability With Interacter
                else if (sen.State == SquareState.AbilityWithInteracterable)
                {
                    interacter = selectedPiece.Piece.AbilityWithInteracterStageOne(table, sen);

                    // If the piece is hiddenly frozen, ability with interacter stage one returns null. Then cur should also be null.
                    if (interacter == null)
                        selectedPiece = null;

                    // Don't check for gameEnded
                    return;
                }

                // None Move
                else if (sen.State == SquareState.Moveable && selectedPiece != sen)
                {
                    selectedPiece.Piece.Move(table, sen.Row, sen.Column, ref turn);
                    UpdateHistory();

                    UpdatePits();
                    selectedPiece = null;
                }

                // If another friendly piece is chosen
                else if (selectedPiece != sen && sen.Piece != null && selectedPiece.Piece.Player == sen.Piece.Player)
                {
                    selectedPiece = null;
                    Repaint();
                    goto tunnel1; // Behave as if the cur was null and go back to the top of the LMDown method
                }

                // Unvalid square chosen
                else
                {
                    selectedPiece = null;
                    Repaint();
                }
            }

            // Update gameEnded
            bool whiteLotusIsOnBoard = false;
            bool blackLotusIsOnBoard = false;
            for (int i = 1; i <= nr; i++)
                for (int j = 1; j <= nc; j++)
                {
                    if (table[i, j].Piece == null || !(table[i, j].Piece is Lotus)) continue;

                    // If either lotus is on the board
                    if (table[i, j].Piece.Player == PlayerType.White)
                        whiteLotusIsOnBoard = true;
                    else
                        blackLotusIsOnBoard = true;

                    // If a lotus reaches the last row for either player, game ends.
                    if (table[i, j].Piece.Player == PlayerType.White && i == nr || table[i, j].Piece.Player == PlayerType.Black && i == 1)
                        gameEnded = true;
                }
            // If any lotus is removed, game ends.
            if (!whiteLotusIsOnBoard || !blackLotusIsOnBoard)
                gameEnded = true;
        }

        public void Select(Square sen)
        {
            if (gameEnded) return;

            // If a valid piece is chosen, paint the possible moves for preview.
            if (sen.Piece != null && (!turnConstraintsEnabled ||
                sen.Piece.Player == PlayerType.White && (turn % 4 == 0 || turn % 4 == 3) || sen.Piece.Player == PlayerType.Black && (turn % 4 == 1 || turn % 4 == 2)))
            {
                sen.Piece.PossibleMoves(table, turn);
                selectedPiece = sen;
            }
            // If a non-valid square is chosen, do nothing
            else
            {
                // Explicitly kick other (cur == null) cases, don't merge 2 if statements.
            }
        }

        public bool Activate(Square sen)
        {
            return false;
        }

        #endregion

        #region Tool methods

        /// <summary>
        /// Remove the pieces that are on the pits and that have no adjacent friendly piece from the game.
        /// </summary>
        public void UpdatePits()
        {
            int[,] pits = { { 3, 2 }, { 3, 5 }, { 5, 2 }, { 5, 5 } };
            for (int i = 0; i < 4; i++)
            {
                if (table[pits[i, 0], pits[i, 1]].Piece != null &&
                   (table[pits[i, 0] + 1, pits[i, 1]].Piece == null || table[pits[i, 0] + 1, pits[i, 1]].Piece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0], pits[i, 1] + 1].Piece == null || table[pits[i, 0], pits[i, 1] + 1].Piece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0] - 1, pits[i, 1]].Piece == null || table[pits[i, 0] - 1, pits[i, 1]].Piece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0], pits[i, 1] - 1].Piece == null || table[pits[i, 0], pits[i, 1] - 1].Piece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0] + 1, pits[i, 1]].PseudoPiece == null || table[pits[i, 0] + 1, pits[i, 1]].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0], pits[i, 1] + 1].PseudoPiece == null || table[pits[i, 0], pits[i, 1] + 1].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0] - 1, pits[i, 1]].PseudoPiece == null || table[pits[i, 0] - 1, pits[i, 1]].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0], pits[i, 1] - 1].PseudoPiece == null || table[pits[i, 0], pits[i, 1] - 1].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player))
                {
                    table[pits[i, 0], pits[i, 1]].Piece = null;
                }
                // If the piece is Pseudo. (For PseudoPiece concept, refer to Square.cs or the game manual)
                if (table[pits[i, 0], pits[i, 1]].PseudoPiece != null &&
                   (table[pits[i, 0] + 1, pits[i, 1]].Piece == null || table[pits[i, 0] + 1, pits[i, 1]].Piece.Player != table[pits[i, 0], pits[i, 1]].PseudoPiece.Player) &&
                   (table[pits[i, 0], pits[i, 1] + 1].Piece == null || table[pits[i, 0], pits[i, 1] + 1].Piece.Player != table[pits[i, 0], pits[i, 1]].PseudoPiece.Player) &&
                   (table[pits[i, 0] - 1, pits[i, 1]].Piece == null || table[pits[i, 0] - 1, pits[i, 1]].Piece.Player != table[pits[i, 0], pits[i, 1]].PseudoPiece.Player) &&
                   (table[pits[i, 0], pits[i, 1] - 1].Piece == null || table[pits[i, 0], pits[i, 1] - 1].Piece.Player != table[pits[i, 0], pits[i, 1]].PseudoPiece.Player) &&
                   (table[pits[i, 0] + 1, pits[i, 1]].PseudoPiece == null || table[pits[i, 0] + 1, pits[i, 1]].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0], pits[i, 1] + 1].PseudoPiece == null || table[pits[i, 0], pits[i, 1] + 1].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0] - 1, pits[i, 1]].PseudoPiece == null || table[pits[i, 0] - 1, pits[i, 1]].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player) &&
                   (table[pits[i, 0], pits[i, 1] - 1].PseudoPiece == null || table[pits[i, 0], pits[i, 1] - 1].PseudoPiece.Player != table[pits[i, 0], pits[i, 1]].Piece.Player))
                {
                    // There isn't another piece on the pseudo piece
                    if (table[pits[i, 0], pits[i, 1]].Piece == table[pits[i, 0], pits[i, 1]].PseudoPiece)
                    {
                        table[pits[i, 0], pits[i, 1]].PseudoPiece = null;
                        table[pits[i, 0], pits[i, 1]].Piece = null;
                    }
                    // There is another piece on the pseudo piece
                    else
                    {
                        table[pits[i, 0], pits[i, 1]].PseudoPiece = null;
                    }
                }
            }
        }

        /// <summary>
        /// Record the game to have an undo button
        /// </summary>
        public void UpdateHistory()
        {
            for (int i = 1; i <= nr; i++)
                for (int j = 1; j <= nc; j++)
                    history[turn, i, j] = (Square)table[i, j].Clone();
        }

        /// <summary>
        /// Repaint the board to default colors
        /// </summary>
        public void Repaint()
        {
            for (int i = 1; i <= nr; i++)
                for (int j = 1; j <= nc; j++)
                    table[i, j].State = SquareState.None;
        }

        #endregion
    }
}
