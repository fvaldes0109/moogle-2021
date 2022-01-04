namespace MoogleEngine;

// Clase destinada a la obtencion de la raiz de una palabra
public static class Stemming {

    // Pronombres del paso 0 del stemming
    static string[] pronouns = new string[] { "me", "se", "sela", "selo", "selas", "selos", "la", "le",
                                              "lo", "las", "les", "los", "nos", "te", "os", "telo",
                                              "tela", "telos", "telas", "melo", "mela", "melos", "melas",
                                              "noslo", "nosla", "noslos", "noslas", "oslo", "osla",
                                              "oslos", "oslas" };

    // Previos a los pronombres
    static string[] prePronounB = new string[] { "iendo", "ando", "ar", "er", "ir", "an", "en", "e", "a" };
    static string[] prePronounC = new string[] { "yendo" };

    // Sufijos estandares del paso 1
    static string[] standSuffA = new string[] { "anza", "anzas", "ico", "ica", "icos", "icas", "ismo",
    "ismos", "able", "ables", "ible", "ibles", "ista", "istas", "oso", "osa", "osos", "osas", "amiento",
    "amientos", "imiento", "imientos", "ero", "era", "eros", "eras" };
    static string[] standSuffB = new string[] { "adora", "ador", "acion", "adoras", "adores", "aciones",
    "ante", "antes", "ancia", "ancias" };
    static string[] standSuffC = new string[] { "logia", "logias" };
    static string[] standSuffD = new string[] { "ucion", "uciones" };
    static string[] standSuffE = new string[] { "encia", "encias" };
    static string[] standSuffF = new string[] { "amente" };
    static string[] standSuffF1 = new string[] { "os", "ic", "ad" };
    static string[] standSuffG = new string[] { "mente" };
    static string[] standSuffG1 = new string[] { "ante", "able", "ible" };
    static string[] standSuffH = new string[] { "idad", "idades" };
    static string[] standSuffH1 = new string[] { "abil", "ic", "iv" };
    static string[] standSuffI = new string[] { "iva", "ivo", "ivas", "ivos" };
    static string[] standSuffJ = new string[] { "ion", "iones", "or", "ores" };
    static string[] standSuffK = new string[] { "ente" };

    // Sufijos verbales del paso 2
    static string[] verbsY = new string[] { "ya", "ye", "yan", "yen", "yeron", "yendo", "yo", "yas",
    "yes", "yais", "yamos" };
    static string[] verbsB1 = new string[] { "en", "es", "eis", "emos", "emonos", "emosle", "emosles",
    "emoslo", "emoslos", "emosla", "emoslas" };
    static string[] verbsB2 = new string[] { "arian", "arias", "aran", "aras", "ariais", "aria", "areis",
    "ariamos", "aremos", "ara", "are", "erian", "erias", "eran", "eras", "eriais", "eria", "ereis",
    "eriamos", "eremos", "era", "ere", "irian", "irias", "irias", "iran", "iras", "iriais", "iria",
    "ireis", "iriamos", "iremos", "ira", "ire", "aba", "ada", "ida", "ia", "ara", "iera", "ad", "ed",
    "ase", "iese", "aste", "iste", "an", "aban", "ian", "ieran", "asen", "iesen", "aron", "ieron", "ado",
    "ido", "ando", "iendo", "io", "ar", "er", "ir", "as", "abas", "adas", "idas", "ias", "aras", "ieras",
    "ases", "ieses", "is", "ais", "abais", "iais", "arais", "ierais", "aseis", "ieseis", "asteis",
    "isteis", "ados", "idos", "amos", "abamos", "iamos", "imos", "aramos", "ieramos", "iesemos", "asemos",
    "an" };

    // Sufijos residuales del paso 3
    static string[] residual = new string[] { "os", "a", "o", "i" };

    // Metodo para obtener la verdadera raiz de la palabra
    // Algoritmo: http://snowball.tartarus.org/algorithms/spanish/stemmer.html
    public static string GetRoot(string word) {

        string result = word;

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

        #region Paso 0: Pronombres
        
        string suffix = LongestSuffix(word, pronouns);
        string tillSuffix = word.Substring(0, word.Length - suffix.Length);

        string prevPron = "";
        string prevB = LongestSuffix(tillSuffix, prePronounB);
        string prevC = LongestSuffix(tillSuffix, prePronounC);

        if (prevB != "") prevPron = prevB;
        else if (prevC != "" && tillSuffix.Length - prevC.Length >= 1 && tillSuffix[tillSuffix.Length - prevC.Length - 1] == 'u') {
            prevPron = prevC;
        }

        if (prevPron != "" && tillSuffix != "") {
            result = tillSuffix;
        }

        #endregion

        #region Paso 1: Sufijos estandar

        string mark1 = result; // Para comprobar si se realizo el paso 1

        string stSufA = LongestSuffix(result, standSuffA);
        string stSufB = LongestSuffix(result, standSuffB);
        string stSufC = LongestSuffix(result, standSuffC);
        string stSufD = LongestSuffix(result, standSuffD);
        string stSufE = LongestSuffix(result, standSuffE);
        string stSufF = LongestSuffix(result, standSuffF);
        string stSufG = LongestSuffix(result, standSuffG);
        string stSufH = LongestSuffix(result, standSuffH);
        string stSufI = LongestSuffix(result, standSuffI);
        string stSufJ = LongestSuffix(result, standSuffJ);
        string stSufK = LongestSuffix(result, standSuffK);

        if (stSufA != "" && result.Length - stSufA.Length >= R1) { // R1 modificacion mia
            result = result.Substring(0, result.Length - stSufA.Length);
        }
        else if (stSufB != "" && result.Length - stSufB.Length >= R1) { // R1 modificacion mia
            result = result.Substring(0, result.Length - stSufB.Length);
            if (result.EndsWith("ic") && result.Length - 2 >= R1) { // R1 modificacion mia
                result = result.Substring(0, result.Length - 2);
            }
        }
        else if (stSufC != "" && result.Length - stSufC.Length >= R1) { // R1 mio
            result = result.Substring(0, result.Length - stSufC.Length);
            result += "log";
        }
        else if (stSufD != "" && result.Length - stSufD.Length >= R1) { // R1 mio
            result = result.Substring(0, result.Length - stSufD.Length);
            result += "u";
        }
        else if (stSufE != "" && result.Length - stSufE.Length >= R1) { // R1 mio
            result = result.Substring(0, result.Length - stSufE.Length);
        }
        else if (stSufF != "" && result.Length - stSufF.Length >= R1) {
            result = result.Substring(0, result.Length - stSufF.Length);
            if (result.EndsWith("iv") && result.Length - 2 >= R1) { // R1 mio
                result = result.Substring(0, result.Length - 2);
                if (result.EndsWith("at") && result.Length - 2 >= R1) { // R1 mio
                    result = result.Substring(0, result.Length - 2);
                }
            }
            else {
                string suff = LongestSuffix(result, standSuffF1);
                if (suff != "" && result.Length - suff.Length >= R1) { // R1 mio
                    result = result.Substring(0, result.Length - suff.Length);
                }
            }
        }
        else if (stSufG != "" && result.Length - stSufG.Length >= R1) { // R1 mio
            result = result.Substring(0, result.Length - stSufG.Length);
            string suff = LongestSuffix(result, standSuffG1);
            if (suff != "" && result.Length - suff.Length >= RV) { // RV mio
                result = result.Substring(0, result.Length - suff.Length);
            }
        }
        else if (stSufH != "" && result.Length - stSufH.Length >= R1) { // R1 mio
            result = result.Substring(0, result.Length - stSufH.Length);
            string suff = LongestSuffix(result, standSuffH1);
            if (suff != "" && result.Length - suff.Length >= R1) { // R1 mio
                result = result.Substring(0, result.Length - suff.Length);
            }
        }
        else if (stSufI != "" && result.Length - stSufI.Length >= R2) {
            result = result.Substring(0, result.Length - stSufI.Length);
            if (result.EndsWith("at") && result.Length - 2 >= R2) {
                result = result.Substring(0, result.Length - 2);
            }
        }
        else if (stSufJ != "" && result.Length - stSufJ.Length >= R1) {
            if (result[result.Length - stSufJ.Length - 1] == 's') {
                result = result.Substring(0, result.Length - stSufJ.Length);
            }
        }
        else if (stSufK != "" && result.Length - stSufK.Length >= R1) {
            result = result.Substring(0, result.Length - stSufK.Length);
        }

        #endregion

        #region Paso 2: Sufijos verbales

        // Si no se realizo el paso 1, realizar el 2
        if (result == mark1) {

            string mark2a = result; // Para saber si se aplico el paso 2a

            string suffY = LongestSuffix(result, verbsY);

            if (suffY != "" && result.Length - suffY.Length >= RV && result.Length - suffY.Length >= 1 && result[result.Length - suffY.Length - 1] == 'u') {
                result = result.Substring(0, result.Length - suffY.Length);
            }

            // Si no se realizo el paso 2a, realizar el 2b
            if (mark2a == result) {

                string suffB1 = LongestSuffix(result, verbsB1);
                string suffB2 = LongestSuffix(result, verbsB2);

                if (suffB1 != "" && result.Length - suffB1.Length >= RV && suffB2.Length < suffB1.Length) {
                    result = result.Substring(0, result.Length - suffB1.Length);
                    if (result.EndsWith("gu")) {
                        result = result.Substring(0, result.Length - 1);
                    }
                }
                else if (suffB2 != "" && result.Length - suffB2.Length >= RV) {
                    result = result.Substring(0, result.Length - suffB2.Length);
                }
            }
        }

        #endregion

        #region Paso 3: Sufijos residuales

        string resSuf = LongestSuffix(result, residual);

        if (resSuf != "" && result.Length - resSuf.Length >= RV) {
            result = result.Substring(0, result.Length - resSuf.Length);
        }
        else if (result.EndsWith('e')) {
            result = result.Substring(0, result.Length - 1);
            if (result.EndsWith("gu") && result.Length - 1 >= RV) {
                result = result.Substring(0, result.Length - 1);
            }
        }

        #endregion

        return result;
    }

    // Dada una palabra y una lista de sufijos, devuelve el mayor sufijo
    static string LongestSuffix(string word, string[] suffixes) {
        
        int maxLen = 0;
        string maxSuff = "";
        foreach (string suffix in suffixes) {
            if (suffix.Length > maxLen && word.EndsWith(suffix)) {
                maxLen = suffix.Length;
                maxSuff = suffix;
            }
        }
        return maxSuff;
    }
}