﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ludorill_server_core.Exceptions;

namespace ludorill_server_core
{
    class Match
    {
        public int id;
        private Dictionary<Player, Color> playersToColors;
        private Dictionary<Player, Animal> animalSelections;
        private Board board;
        private Player currentPlayer;
        private int lastDiceRoll;
        private Color lastSelectedColor = Color.BLUE;

        public Match(int id)
        {
            this.id = id;
            playersToColors = new Dictionary<Player, Color>();
            animalSelections = new Dictionary<Player, Animal>();
            board = new Board();
        }

        /*
         * Ejecuta el movimiento y devuelve el numero de pasos dado.
         */
        public int PlayTurn(Player p, int ficha)
        {
            if (currentPlayer != p)
                throw new NotYourTurnException();

            if (!CanMovePiece(p, lastDiceRoll, ficha))
                throw new PieceCantBeMovedException();

            playersToColors.TryGetValue(p, out Color playerColor);
            board.Move(playerColor, ficha, lastDiceRoll);
            return lastDiceRoll;
        }

        public int RollDice(Player p)
        {
            if (currentPlayer != p)
                throw new NotYourTurnException();

            Random r = new Random();
            lastDiceRoll = r.Next(5, 7);
            return lastDiceRoll;
        }

        public void Join(Player p, Animal selection)
        {
            if (playersToColors.Count == 4)
                throw new MatchIsFullException();

            if (IsAlreadySelected(selection))
                throw new AnimalAlreadySelectedException();

            // TODO: Cambiar esto por sistema de que cada jugador rollee y el mayor es el primero
            if (playersToColors.Count == 0)
                currentPlayer = p;

            playersToColors.Add(p, lastSelectedColor++);
            animalSelections.Add(p, selection);
        }

        public bool Has(Player player)
        {
            return playersToColors.TryGetValue(player, out Color c);
        }

        public bool IsAlreadySelected(Animal animal)
        {
            foreach(KeyValuePair<Player, Animal> keyPair in animalSelections)
            {
                if (keyPair.Value == animal)
                    return true;
            }
 
            return false;
        }

        public List<Player> GetPlayers()
        {
            List<Player> res = new List<Player>();
            foreach (KeyValuePair<Player, Color> keyValue in playersToColors)
                res.Add(keyValue.Key);

            return res;
        }

        public Color GetPlayerColor(Player p)
        {
            bool found = playersToColors.TryGetValue(p, out Color c);
            if (found)
            {
                return c;
            } else
            {
                // TODO: Throw Exception?
                return Color.EMPTY;
            }           
        }

        public List<int> MovablePieces(Player p, int diceRoll)
        {
            // TODO: Validar que se haya conseguido el valor
            playersToColors.TryGetValue(p, out Color c);
            return board.MovablePieces(c, diceRoll);
        }

        private bool CanMovePiece(Player p, int diceRoll, int index)
        {
            var movable = MovablePieces(p, diceRoll);
            foreach (int m in movable)
            {
                if (m == index)
                    return true;
            }

            return false;
        }

    }
}
