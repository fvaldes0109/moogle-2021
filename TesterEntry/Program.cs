﻿using MoogleEngine;

public static class Program {
    public static void Main(string[] args) {

        int amount = 8;

        if (args.Length >= 2) {
            if (args[0] == "--amount") amount = int.Parse(args[1]);
        } 

        Moogle.Init();

        var words = Moogle.FrequentWords();

        int docAmount = Moogle.DocumentAmount();

        int cont = 0;

        for (int k = 0; k < amount; k++) {

            Console.WriteLine("Frecuencia: {0}/{1} --> {2}%", docAmount - k, docAmount, (float)((docAmount - k) * 100f / docAmount));

            for (int i = 0; i < words.Count; i++) {
                if (words[i].Item2 == docAmount - k) {
                    Console.WriteLine((cont + 1) + "-Palabra: {0} - Ocurrencias: {1} - Total: {2}", words[i].Item1, words[i].Item3, words[i].Item4);
                    cont++;
                }
            }
            Console.WriteLine();
        }
    }
}


