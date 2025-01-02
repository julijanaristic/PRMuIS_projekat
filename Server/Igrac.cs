using System;

public class Igrac
{
	public int BrojIgraca { get; set; }
	public string Ime {  get; set; }
	public int[] BrojPoenaPoIgrama { get; set; }

	public Igrac(int brojIgraca, string ime, int brojIgara)
	{
		BrojIgraca = brojIgraca;
		Ime = ime;
		BrojPoenaPoIgrama = new int[brojIgara];
	}



}
