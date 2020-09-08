using System;
public delegate void MyHandler1(object sender, MyEvent e);
public class MyEvent : EventArgs
{
    public string message;
    public int ganador;
    public int perdedor;
    public string winnername;
    public string losername;
    public string loserreason;
    public string loserins;
    public int round;
    public int ciclos;
}
   

