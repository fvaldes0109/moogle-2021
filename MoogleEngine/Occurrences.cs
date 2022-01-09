namespace MoogleEngine;

// Representa las ocurrencias de alguna palabra en un documento especifico
public class Occurrences {
    
    float tfidf;
    List<int> startPos = new List<int> (); // Posiciones iniciales de cada ocurrencia
    
    public void Push(int startPos) { // Agrega una ocurrencia en el documento
        this.startPos.Add(startPos);
    }

    public float Relevance { // La relevancia de la palabra en este documento
        get {
            return tfidf;
        }
        set {
            tfidf = (value == 0 ? 0.000000001f : value);
        }
    } 

    public List<int> StartPos { get { return this.startPos; } }
}