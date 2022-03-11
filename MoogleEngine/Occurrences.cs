namespace MoogleEngine;

// Representa las ocurrencias de alguna palabra en un documento especifico
public class Occurrences {
    
    float tfidf;
    
    public Occurrences() {
        this.StartPos = new List<int>();
    }

    // La relevancia de la palabra en este documento
    public float Relevance {
        get {
            return tfidf;
        }
        set {
            // A las palabras que aparezcan demasiado se les dara un valor muy pequeño
            tfidf = (value == 0 ? 0.000000001f : value);
        }
    } 

    // Posiciones iniciales (en bytes) de cada ocurrencia
    public List<int> StartPos { get; set; }
}