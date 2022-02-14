using System.Text;
namespace MoogleEngine;

// Clase destinada a la obtencion de la raiz de una palabra
public static class Stemming {

    // Pronombres del paso 0 del stemming
    static string[] pronouns = new string[] { "noslos", "noslas", "telos", "telas", "melos", "melas", 
                                              "noslo", "nosla", "oslos", "oslas", "selas", "selos",
                                              "sela", "selo", "telo", "tela", "melo", "mela", "oslo",
                                              "osla", "las", "les", "los", "nos", "la", "le", "lo",
                                              "te", "os", "me", "se" };

    // Previos a los pronombres
    static string[] prePronounA = new string[] { "iéndo", "ándo", "ár", "ér", "ír", "án", "én", "ád", "é", "á" };
    static string[] prePronounB = new string[] { "iendo", "ando", "ar", "er", "ir", "an", "en", "ad", "e", "a" };
    static string[] prePronounC = new string[] { "yendo" };

    // Sufijos estandares del paso 1
    static string[] standSuffA = new string[] { "amientos", "imientos", "amiento", "imiento", "ismos",
                                                "anzas", "ables", "ibles", "istas", "anza", "icos", "icas",
                                                "able",  "ible",  "ista", "osos", "osas", "eros", "eras",
                                                "ismo", "ico", "ica", "oso", "osa", "ero", "era" };
       
    static string[] standSuffB = new string[] { "aciones", "adoras", "adores", "ancias", "adora", "ación",
                                                "antes", "ancia", "ador", "ante" };
    static string[] standSuffC = new string[] { "logías", "logía" };
    static string[] standSuffD = new string[] { "uciones", "ución" };
    static string[] standSuffE = new string[] { "encias", "encia" };
    static string[] standSuffF = new string[] { "amente" };
    static string[] standSuffF1 = new string[] { "os", "ic", "ad" };
    static string[] standSuffG = new string[] { "mente" };
    static string[] standSuffG1 = new string[] { "ante", "able", "ible" };
    static string[] standSuffH = new string[] { "idades", "idad" };
    static string[] standSuffH1 = new string[] { "abil", "ic", "iv" };
    static string[] standSuffI = new string[] { "ivas", "ivos", "iva", "ivo" };
    static string[] standSuffJ = new string[] { "iones", "ores", "ion", "or" };
    static string[] standSuffK = new string[] { "ente" };

    // Sufijos verbales del paso 2
    static string[] verbsY = new string[] { "yeron", "yendo", "yamos", "yais", "yáis", "yas", "yes", "yan", "yen",                                    
                                            "ya", "ye", "yo", "yó" };
    static string[] verbsB1 = new string[] { "émosles", "émoslos", "émoslas", "émosle", "émoslo", "émosla",
                                             "émonos", "emos", "éis", "en", "es" };
    static string[] verbsB2 = new string[] { "aríamos", "eríamos", "iríamos", "iéramos", "iésemos", "aríais",
                                             "aremos", "eríais", "eremos", "iríais", "iremos", "ierais",
                                             "ieseis", "asteis", "isteis", "ábamos", "áramos", "áremos",
                                             "ásemos", "arían", "arías", "aréis", "erían", "erías", "eréis",
                                             "irían", "irías", "irías", "iréis", "ieran", "iesen", "ieron",
                                             "iendo", "ieras", "ieses", "abais", "arais", "aseis", "íamos",
                                             "arán", "arás", "aría", "erán", "erás", "ería", "irán", "irás",
                                             "iría", "iera", "iese", "aste", "iste", "aban", "aran", "asen",
                                             "ando", "aron", "abas", "adas", "idas", "aras", "ares", "ases",
                                             "íais", "ados", "idos", "amos", "imos", "ará", "aré", "erá",
                                             "eré", "irá", "iré", "aba", "ada", "ida", "ara", "are", "ase",
                                             "ían", "ado", "ido", "ías", "áis", "ía", "ad", "ed", "id", "an",
                                             "ió", "ar", "er", "ir", "as", "ís", "an", "án" };

    // Sufijos residuales del paso 3
    static string[] residual = new string[] { "os", "a", "o", "í", "ó", "á" };

    // Metodo para obtener la verdadera raiz de la palabra
    // Algoritmo: http://snowball.tartarus.org/algorithms/spanish/stemmer.html
    public static string GetRoot(string word) {

        StringBuilder result = new StringBuilder(word);

        #region Calculando R1, R2 y RV

        // R1: Region desde la 1ra no vocal siguiendo a una vocal
        // R2: A partir de R1, region desde la 1ra no vocal siguiendo a una vocal
        int R1 = word.Length, R2 = word.Length, RV = word.Length;

        bool firstVowel = false; // Si se encontro la 1ra vocal
        bool vowelInR1 = false; // Si se encontro la 1ra vocal en R1
        for (int i = 0; i < word.Length; i++) {

            bool vowel = StringParser.IsVowel(word[i]);
            if (vowel) firstVowel = true;

            if (!vowel && firstVowel && R1 == word.Length) R1 = i + 1;

            // Si ya se encontro R1, calculamos R2
            if (R1 != word.Length) {

                if (vowel) vowelInR1 = true;
                if (!vowel && vowelInR1 && R2 == word.Length) R2 = i + 1;
            }

            // Hallando RV
            if (i > 1 && RV == word.Length) {
                
                if (!StringParser.IsVowel(word[1]) && vowel) RV = i + 1;
                else if (StringParser.IsVowel(word[0]) && !vowel) RV = i + 1;
                else RV = 3;
            }
        }

        #endregion

        #region Paso -1: Parche para adverbios terminados en 'te'

        string stSufF = LongestSuffix(result, standSuffF);
        if (stSufF != "" && result.Length - stSufF.Length >= R1) {
            result.Remove(result.Length - stSufF.Length, stSufF.Length);
            if (result.Length - 2 >= R1 && result[^2] == 'i' && result[^1] == 'v') { // R1 mio
                result.Remove(result.Length - 2, 2);
                if (result.Length - 2 >= R1 && result[^1] == 'd') { // R1 mio
                    result.Remove(result.Length - 2, 2);
                }
            }
            else {
                string suff = LongestSuffix(result, standSuffF1);
                if (suff != "" && result.Length - suff.Length >= R1) { // R1 mio
                    result.Remove(result.Length - suff.Length, suff.Length);
                }
            }
        }
        else {
            
            string stSufG = LongestSuffix(result, standSuffG);
            if (stSufG != "" && result.Length - stSufG.Length >= R1) { // R1 mio
                result.Remove(result.Length - stSufG.Length, stSufG.Length);
                string suff = LongestSuffix(result, standSuffG1);
                if (suff != "" && result.Length - suff.Length >= RV) { // RV mio
                    result.Remove(result.Length - suff.Length, suff.Length);
                }
            }
        }

        #endregion

        #region Paso 0: Pronombres
        
        string suffix = LongestSuffix(result, pronouns);
        string tillSuffix = result.ToString().Substring(0, result.Length - suffix.Length);

        string prevPron = "";

        if (result.Length - suffix.Length >= R1) {

            string prevA = LongestSuffix(tillSuffix, prePronounA);
            if (prevA != "" && tillSuffix.Length - prevA.Length >= RV) prevPron = prevA;
            else {
                string prevB = LongestSuffix(tillSuffix, prePronounB);
                if (prevB != "" && tillSuffix.Length - prevB.Length >= RV) prevPron = prevB;
                else {
                    string prevC = LongestSuffix(tillSuffix, prePronounC);
                    if (prevC != "" && tillSuffix.Length - prevC.Length >= RV && tillSuffix[tillSuffix.Length - prevC.Length - 1] == 'u') {
                        prevPron = prevC;
                    }
                }
            }

            if (prevPron != "" && tillSuffix != "") {
                result.Remove(tillSuffix.Length, suffix.Length);
                if (prevA != "") result = new StringBuilder(StringParser.ParseAccents(result.ToString()));
            }
        }

        #endregion

        #region Paso 1: Sufijos estandar

        string mark1 = result.ToString(); // Para comprobar si se realizo el paso 1

        string stSufA = LongestSuffix(result, standSuffA);
        if (stSufA != "" && result.Length - stSufA.Length >= R1) { // R1 modificacion mia
            result.Remove(result.Length - stSufA.Length, stSufA.Length);
        }
        else {

            string stSufB = LongestSuffix(result, standSuffB);
            if (stSufB != "" && result.Length - stSufB.Length >= R1) { // R1 modificacion mia
                result.Remove(result.Length - stSufB.Length, stSufB.Length);
                if (result.Length - 2 >= R1 && result[^2] == 'i' && result[^1] == 'c') { // R1 modificacion mia
                    result.Remove(result.Length - 2, 2);
                }
            }
            else {

                string stSufC = LongestSuffix(result, standSuffC);
                if (stSufC != "" && result.Length - stSufC.Length >= R1) { // R1 mio
                    result.Remove(result.Length - stSufC.Length, stSufC.Length);
                    result.Append("log");
                }
                else {

                    string stSufD = LongestSuffix(result, standSuffD);
                    if (stSufD != "" && result.Length - stSufD.Length >= R1) { // R1 mio
                        result.Remove(result.Length - stSufD.Length, stSufD.Length);
                        result.Append("u");
                    }
                    else {
                        
                        string stSufE = LongestSuffix(result, standSuffE);
                        if (stSufE != "" && result.Length - stSufE.Length >= R1) { // R1 mio
                            result.Remove(result.Length - stSufE.Length, stSufE.Length);
                        }
                        else {
                            string stSufH = LongestSuffix(result, standSuffH);
                            if (stSufH != "" && result.Length - stSufH.Length >= R1) { // R1 mio
                                result.Remove(result.Length - stSufH.Length, stSufH.Length);
                                string suff = LongestSuffix(result, standSuffH1);
                                if (suff != "" && result.Length - suff.Length >= R1) { // R1 mio
                                    result.Remove(result.Length - suff.Length, suff.Length);
                                }
                            }
                            else {
                                
                                string stSufI = LongestSuffix(result, standSuffI);
                                if (stSufI != "" && result.Length - stSufI.Length >= R2) {
                                    result.Remove(result.Length - stSufI.Length, stSufI.Length);
                                    if (result.Length - 2 >= R2 && result[^2] == 'a' && result[^1] == 't') {
                                        result.Remove(result.Length - 2, 2);
                                    }
                                }
                                else {
                                    
                                    string stSufJ = LongestSuffix(result, standSuffJ);
                                    if (stSufJ != "" && result.Length - stSufJ.Length >= R1) {
                                        if (result[result.Length - stSufJ.Length - 1] == 's') {
                                            result.Remove(result.Length - stSufJ.Length, stSufJ.Length);
                                        }
                                    }
                                    else {

                                        string stSufK = LongestSuffix(result, standSuffK);
                                        if (stSufK != "" && result.Length - stSufK.Length >= R1) {
                                            result.Remove(result.Length - stSufK.Length, stSufK.Length);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Paso 2: Sufijos verbales

        // Si no se realizo el paso 1, realizar el 2
        if (result.ToString() == mark1) {

            string mark2a = result.ToString(); // Para saber si se aplico el paso 2a

            string suffY = LongestSuffix(result, verbsY);

            if (suffY != "" && result.Length - suffY.Length >= RV && result.Length - suffY.Length >= 1 && result[result.Length - suffY.Length - 1] == 'u') {
                result.Remove(result.Length - suffY.Length, suffY.Length);
            }

            // Si no se realizo el paso 2a, realizar el 2b
            if (mark2a == result.ToString()) {

                string suffB1 = LongestSuffix(result, verbsB1);
                string suffB2 = LongestSuffix(result, verbsB2);

                if (suffB1 != "" && result.Length - suffB1.Length >= RV && suffB2.Length < suffB1.Length) {
                    result.Remove(result.Length - suffB1.Length, suffB1.Length);
                    if (result[^2] == 'g' && result[^1] == 'u') {
                        result.Remove(result.Length - 1, 1);
                    }
                }
                else if (suffB2 != "" && result.Length - suffB2.Length >= RV) {
                    result.Remove(result.Length - suffB2.Length, suffB2.Length);
                }
            }
        }

        #endregion

        #region Paso 3: Sufijos residuales

        string resSuf = LongestSuffix(result, residual);

        if (resSuf != "" && result.Length - resSuf.Length >= RV) {
            result = result.Remove(result.Length - resSuf.Length, resSuf.Length);
        }
        else if ((result[^1] == 'e' || result[^1] == 'é') && result.Length - 1 >= RV) {
            result.Remove(result.Length - 1, 1);
            if (result.Length - 1 >= RV && result[^2] == 'g' && result[^1] == 'u') {
                result.Remove(result.Length - 1, 1);
            }
        }

        #endregion

        return StringParser.ParseAccents(result.ToString());
    }

    // Dada una palabra y una lista de sufijos ordenados por longitud, devuelve el mayor sufijo
    static string LongestSuffix(string word, string[] suffixes) {
        
        foreach (string suffix in suffixes) {
            if (word.EndsWith(suffix)) {
                return suffix;
            }
        }
        return "";
    }

    // Sobrecarga con StringBuilder
    static string LongestSuffix(StringBuilder word, string[] suffixes) {
        
        string strWord = word.ToString();

        foreach (string suffix in suffixes) {
            if (strWord.EndsWith(suffix)) {
                return suffix;
            }
        }
        return "";
    }
}