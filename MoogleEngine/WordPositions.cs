namespace MoogleEngine;

// Dado un arreglo de PartialItem, organiza las posiciones de las palabras
public class WordPositions {
    
    public WordPositions() {
        this.Positions = new SortedSet<int> ();
        this.Words = new Dictionary<int, string>();
        this.Differents = new HashSet<string>();
    }

    // Todas las posiciones con palabras deseadas
    public SortedSet<int> Positions { get; private set; }

    // La palabra que esta en cada posicion
    public Dictionary<int, string> Words { get; private set; }

    // Para saber la cantidad de palabras distintas
    public HashSet<string> Differents { get; private set; }

    // Agrega un nuevo arreglo de posiciones a la lista actual
    public void Insert(string word, int[] posArray) {

        foreach (int pos in posArray) {
            
            if (!(this.Positions.Contains(pos))) {
                this.Positions.Add(pos);
                this.Words.Add(pos, word);
                this.Differents.Add(word);
            }
        }
    }
}