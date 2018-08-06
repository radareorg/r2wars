using System;
public delegate void MyHandler1(object sender, MyEvent e);
public class MyEvent : EventArgs
{
    public string message;
    public int ganador;
    public string winnername;
    public int round;
    public int ciclos;
}
   

