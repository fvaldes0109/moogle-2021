namespace MoogleEngine;

// Para cierto doc y cierta frase, guarda el score total y cuales palabras existen en este documento
public class CumulativeScore {

    public CumulativeScore() {
        this.TotalScore = 0.0f;
        this.Content = new List<PartialItem>();
    }

    // El score acumulado existente en el documento
    public float TotalScore { get; private set; }

    // La lista de palabras de la query (o derivaciones) existentes en el documento
    public List<PartialItem> Content { get; private set; }

    // Agrega una palabra a esta lista y aplica su multiplicador
    public void AddWord(float score, PartialItem partial) {
        this.TotalScore += score * partial.Multiplier;
        this.Content.Add(partial);
    }
}