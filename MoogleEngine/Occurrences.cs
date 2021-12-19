namespace MoogleEngine;

// Representa las ocurrencias de alguna palabra en un documento especifico
public class Occurrences {
    
    List<int> startPos = new List<int> (); // Posiciones iniciales de cada ocurrencia
    
    public Occurrences(int id) {
        this.Id = id;
    }

    public void Push(int startPos) { // Agrega una ocurrencia en el documento
        this.startPos.Add(startPos);
    }

    public int Id { get; private set; } // ID del documento analizado
    public float Relevance { get; private set; } // La relevancia de la palabra en este documento

    public List<int> StartPos { get { return this.startPos; } }
}