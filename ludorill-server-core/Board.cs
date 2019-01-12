using System;
using System.Collections.Generic;
using System.Text;

namespace ludorill_server_core
{
    class Board
    {
        private Dictionary<Color, Piece[]> piecesByColor;
        private Dictionary<Color, Cell> startCellsByColor;
        // int[2] donde [0] = Color y [1] = pieceIndex
        public Queue<int[]> piecesToReturnHome;

        public Board()
        {
            piecesByColor = new Dictionary<Color, Piece[]>();
            startCellsByColor = new Dictionary<Color, Cell>();
            piecesToReturnHome = new Queue<int[]>();

            InitializeBoardCells();
            InitializePieces();
        }

        /*
         * Crea las 4 piezas por cada color y las guarda en el diccionario.
         */
        private void InitializePieces()
        {
            for (int i=0; i < 4; i++)
            {
                Color color = (Color)i;
                piecesByColor.Add(color, Piece.InitializeForColor(color));
            }
        }


        /*
         * Crea los objetos que almacenan el mapa y ademas llena el diccionario startCellsByColor
         * que almacena las salidas por color.
         */
        private void InitializeBoardCells()
        {
            // Creo 52 celdas contiguas, generando una lista enlazada circular
            // conectando el mapa para que pueda ser recorrido.
            Cell first = new Cell();
            Cell aux = first;
            for (int i = 0; i < 51; i++)
            {
                Cell c = new Cell();
                first.regularNext = c;
                first = c;
            }
            first.regularNext = aux;
            // Seleccionamos las celdas que van a ser las iniciales de color
            Cell actualFirst = aux;
            Color color = Color.BLUE;

            for (int i = 0; i < 52; i++)
            {
                // Estas posiciones corresponden a aquellas en las que salen las piezas de cada uno de los
                // colores inicialmente, a cada una de ellas le agregamos las celdas necesarias para hacer
                // el camino al centro del mapa, completando asi la estructura del tablero.
                if (i == 0 || i == 13 || i == 26 || i == 39)
                {
                    actualFirst.color = color++;
                    AddColorCells(actualFirst);
                    startCellsByColor.Add(actualFirst.color, actualFirst);
                }

                actualFirst = actualFirst.regularNext;
            }
        }

        /*
         * Crea el color road (o final road), conectandolo a la celda recibida
         * como parametro.
         */
        private void AddColorCells(Cell initial)
        {           
            Cell next = new Cell(initial.color);
            initial.colorNext = next;
            for (int i = 1; i <= 5; i++)
            {
                Cell nextStep = new Cell(initial.color);
                next.regularNext = nextStep;
                next = next.regularNext;
            }
        }

        public void Move(Color color, int pieceIndex, int diceRoll)
        {
            Piece[] pieces;
            bool inDict = piecesByColor.TryGetValue(color, out pieces);
            if (inDict)
            {
                int movimientos = diceRoll;
                Piece currentPiece = pieces[pieceIndex];
                // Si la pieza no se ha movido, la movemos a la primera posicion y ejecutamos el resto de los pasos
                if (currentPiece.currentPosition == null && diceRoll == 6)
                {
                    startCellsByColor.TryGetValue(currentPiece.color, out Cell startCell);
                    currentPiece.currentPosition = startCell;
                    movimientos--;
                }           

                currentPiece.Move(movimientos);

                // Manejo de colisiones
                /*
                int[] collision = pieceInCell(currentPiece.currentPosition, currentPiece.color);
                if (collision != null)
                {                
                    piecesToReturnHome.Enqueue(collision);
                }
                */
            }
        }

        /*
         * Devuelve la pieza del color indicado que esta actualmente en la posicion indicada, o null si 
         * no hay niguna pieza en la casilla indicada
         */
        private int[] pieceInCell(Cell cell, Color c)
        {
            foreach (Piece[] pArr in piecesByColor.Values)
            {
                for (int i=0; i < pArr.Length; i++)
                {
                    // Si hay una pieza en la celda, y es de otro color
                    if (pArr[i].currentPosition == cell && pArr[i].color != c)
                        return new int[2] {(int)pArr[i].color, i};
                }
            }

            return null;
        }

        /*
         * Devuelve el index de las piezas de un color que se pueden mover
         * dado un diceRoll.
         * Por ejemplo: 
         * - Si todas las fichas estan en la posicion inicial, si se rollea
         *   6, devolveria [0,1,2,3]
         * - Si solo 1 ficha esta dentro del mapa y se rollea algun numero diferente a 6,
         *   entonces solo esa ficha puede moverse por lo que devolveria su index.
         */
        public List<int> MovablePieces(Color c, int diceRoll)
        {
            // TODO: Validar que se consiga
            piecesByColor.TryGetValue(c, out Piece[] pieces);
            List<int> result = new List<int>();
            for(int i=0; i < pieces.Length; i++) 
            {
                Piece p = pieces[i];
                // Si la ficha ya esta en el centro, no se puede mover
                if (p.inCenter)
                    continue;

                // Si la posicion actual es null, significa que estamos en la posicion inicial
                // Por lo que para poder salir, se necesita rollear un 6.
                // Si no es null, significa que ya estamos en alguna posicion del mapa y nos podemos mover.
                if ( (p.currentPosition == null && diceRoll == 6) || p.currentPosition != null)
                    result.Add(i);
            }

            return result;
        }

        /*
         * El color road (o final road) se refiere al camino de celdas de color que van al centro
         * del tablero.
         * 
         * Este metodo devuelve un booleano que indica si una pieza esta en este camino, de manera
         * que el cliente sepa que cuando pueda se meta en este camino sin tener programada explicitamente
         * la logica.
         */
        public bool IsInColorRoad(Color c, int pieceIndex)
        {
            piecesByColor.TryGetValue(c, out Piece[] pieces);

            return pieces[pieceIndex].currentPosition?.color == c;
        }

        /*
         * Devuelve la cantidad de piezas de un color que se encuentran en el centro del tablero,
         * es decir, que ya no se pueden mover mas porque llegaron al objetivo.
         */
        public int AmountOfCenterPiecesBy(Color c)
        {
            piecesByColor.TryGetValue(c, out Piece[] pieces);
            int result = 0;
            foreach (Piece p in pieces)
            {
                if (p.inCenter)
                    result++;
            }

            return result;
        }
    }

    class Piece
    {
        public Color color;
        public int movements = 0;
        public Cell currentPosition;
        public bool inCenter = false;

        public Piece(Color color)
        {
            this.color = color;
        }

        public void Move(int amount)
        {
            if (inCenter)
            {
                Console.WriteLine("Tratando de mover pieza que ya esta en el centro");
                // TODO: Ta vez se deberia lanzar una excepcion
                return;
            }

            for (int i=0; i<amount; i++)
            {
                Console.WriteLine("Current movements: " + movements);
                if (currentPosition.colorNext != null && movements == 52)
                {
                    currentPosition = currentPosition.colorNext;
                } else if (currentPosition.regularNext != null)
                {
                    currentPosition = currentPosition.regularNext;
                }
                movements++;
            }

            if (currentPosition.regularNext == null)
            {
                Console.WriteLine("Pieza {0} llego al centro. Movimientos: {1}", color, movements);
                inCenter = true;
            }
        }


        public static Piece[] InitializeForColor(Color c)
        {
            return new Piece[] { new Piece(c), new Piece(c), new Piece(c), new Piece(c) };
        }
       
    }

    class Cell
    {
        public Color color;
        public Cell regularNext;
        public Cell colorNext;

        public Cell(Color c = Color.EMPTY)
        {
            color = c;
        }
    }

}
