using MoogleEngine;

public static class Program {
    public static void Main(string[] args) {
        
        // Cantidad de grupos de palabras a mostrar por default
        int amount = 8;
        // Buscando la cantidad enviada por el usuario en los args
        if (args.Length >= 2) {
            if (args[0] == "--amount") amount = int.Parse(args[1]);
        }

        Moogle.Init();
        // Tomando la lista de palabras desde MoogleEngine
        var words = Moogle.FrequentWords();
        int docAmount = Moogle.DocumentAmount();

        Console.WriteLine("\nCantidad de palabras distintas: {0}\n", words.Count);

        int i = 0, k = 0;
        while (k < amount) {

            Console.WriteLine("Frecuencia: {0}/{1} --> {2}%", docAmount - k, docAmount, (float)((docAmount - k) * 100f / docAmount));

            while (i < words.Count && words[i].Item2 == docAmount - k) {
                Console.WriteLine((i + 1) + "-Palabra: {0} - Ocurrencias: {1} - Total: {2}", words[i].Item1, words[i].Item3, words[i].Item4);
                i++;
            }
            Console.WriteLine();
            k++;
        }
    }
}


