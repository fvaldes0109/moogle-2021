namespace MoogleEngine;

// Dado un arreglo de PartialItem, organiza las posiciones de las palabras
public class WordPositions {
    
    HashSet<string> differents = new HashSet<string>();

    public WordPositions() {
        this.Positions = new SortedSet<int> ();
        this.Words = new Dictionary<int, string>();
    }

    // Todas las posiciones con palabras deseadas
    public SortedSet<int> Positions { get; private set; }

    // La palabra que esta en cada posicion
    public Dictionary<int, string> Words { get; private set; }

    // Para saber la cantidad de palabras distintas
    public int Differents { get { return differents.Count; } }

    // Agrega un nuevo arreglo de posiciones a la lista actual
    public void Insert(string word, int[] posArray) {

        foreach (int pos in posArray) {
            
            if (!(this.Positions.Contains(pos))) {
                this.Positions.Add(pos);
                this.Words.Add(pos, word);
                this.differents.Add(word);
            }
        }
    }
}