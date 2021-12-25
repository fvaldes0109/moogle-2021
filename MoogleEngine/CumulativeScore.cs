namespace MoogleEngine;

// Para cierto doc y cierta frase, guarda el score total y cuales palabras existen en este documento
public class CumulativeScore {

    public CumulativeScore() {
        this.TotalScore = 0.0f;
        this.Content = new List<PartialItem>();
    }

    public float TotalScore { get; private set; }

    public List<PartialItem> Content { get; private set; }

    public void AddWord(float score, PartialItem partial) {
        this.TotalScore += score;
        this.Content.Add(partial);
    }
}