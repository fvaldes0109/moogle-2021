namespace MoogleEngine;

// Representa las ocurrencias de alguna palabra en un documento especifico
public class Occurrences {
    
    float tfidf;
    List<int> startPos = new List<int> ();
    
    // Agrega una ocurrencia de la en el documento
    public void Push(int startPos) { 
        this.startPos.Add(startPos);
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
    public List<int> StartPos { get { return this.startPos; } }
}