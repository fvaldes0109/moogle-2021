namespace MoogleEngine;

public class Location { // Almacena todos los datos de cada palabra

    Occurrences[] occurrences; // Las apariciones de la palabra en cada documento
    int amount; // La cantidad de documentos en que se encuentra

    public Location(int totalDocs) {
        this.occurrences = new Occurrences[totalDocs];
        amount = 0;
    }

    public int Size { get { return occurrences.Length; } }

    public Occurrences this[int i] {
        get {
            return this.occurrences[i];
        }
        set {
            this.occurrences[i] = value;
        }
    }

    public void Sort() {
        // Array.Sort(this.occurrences, new TFIDFComparer());
        this.occurrences.OrderByDescending(x => x.Relevance);
        RemoveNull();
    }

    void RemoveNull() {
        List<Occurrences> list = new List<Occurrences> ();

        foreach (var item in this.occurrences) {
            if (item != null) list.Add(item);
        }

        this.occurrences = list.ToArray();
    }
}