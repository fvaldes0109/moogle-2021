namespace MoogleEngine;

public class ParsedInput {

    public ParsedInput() {
        this.Words = new List<string>();
        this.Operators = new List<string>();
        this.Tildes = new List<bool>();
    }

    // Las palabras existentes en el query
    public List<string> Words { get; private set; }

    // Los operadores !*^ en el query. Su posicion se asocia con su palabra
    public List<string> Operators { get; private set; }

    // Si la i-esima posicion es true, significa que entre la i-esima palabra y la i+1 hay un ~
    public List<bool> Tildes { get; private set; }

    // Inserta el nuevo operador en la posicion correspondiente
    public void PushOperator(char c) {
        // Si el tamaño es <= entonces este es el 1er operador asociado a esta palabra
        if (this.Operators.Count <= this.Words.Count) {

            // Rellenando los espacios de las palabras que no tenian operadores
            for (int i = 0; i < this.Words.Count - this.Operators.Count; i++) {
                this.Operators.Add("");
            }
            // Agregando el operador a la posicion correspondiente a la palabra
            this.Operators.Add(c.ToString());
        }
        else { // Si ya habian operadores para esta palabra
            this.Operators[this.Operators.Count - 1] += c;
        }
    }

    // Inserta un operador ~ en la posicion correspondiente
    public void PushTilde() {

        // Si no se han parseado palabras, es que el ~ esta al inicio, por lo que se descartara
        // Si ambas longitudes son iguales es que hay mas de un ~ seguido. Se descarta
        if (this.Words.Count == 0 || this.Words.Count == this.Tildes.Count) return;

        // Rellenando los espacios de las palabras que no tenian operadores
        for (int i = this.Tildes.Count; i < this.Words.Count - this.Tildes.Count - 1; i++) {
            this.Tildes.Add(false);
        }
        // Agregando true en la posicion recibida
        this.Tildes.Add(true);
    }

    // Elimina los posibles operadores sobrantes al final e iguala los tamaños de las listas
    public void TrimEnd() {
        // Si no hay ninguna palabra, eliminar todos los operadores
        if (this.Words.Count == 0) {
            this.Operators = new List<string>();
            this.Tildes = new List<bool>();
            return;
        }
        // Eliminando los operadores !*^
        if (this.Operators.Count > this.Words.Count) {
            this.Operators.RemoveAt(this.Operators.Count - 1);
        }
        // Eliminando el operador ~
        if (this.Tildes.Count == this.Words.Count) {
            this.Tildes.RemoveAt(this.Tildes.Count - 1);
        }

        // Ajustando los tamaños
        while (this.Operators.Count < this.Words.Count) {
            this.Operators.Add("");
        }
        while (this.Tildes.Count < this.Words.Count) {
            this.Tildes.Add(false);
        }
    }
}