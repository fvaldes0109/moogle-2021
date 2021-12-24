namespace MoogleEngine;

// Representa las ocurrencias de alguna palabra en un documento especifico
public class Occurrences {
    
    float tfidf;
    List<int> startPos = new List<int> (); // Posiciones iniciales de cada ocurrencia
    
    public Occurrences(int id) {
        this.Id = id;
    }

    public void Push(int startPos) { // Agrega una ocurrencia en el documento
        this.startPos.Add(startPos);
    }

    public int Id { get; private set; } // ID del documento analizado
    public float Relevance { // La relevancia de la palabra en este documento
        get {
            return tfidf;
        }
        set {
            tfidf = (value == 0 ? 0.000001f : value);
        }
    } 

    public List<int> StartPos { get { return this.startPos; } }
}