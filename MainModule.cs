using Nancy;
namespace r2warsTorneo
{
	public class MainModule : NancyModule
	{
		public MainModule()
		{
            Get["/"] = x => View["index.html"];
		}
	}
    /*public class r2warsModule : NancyModule
    {
        public r2warsModule() : base("/r2wars")
        {
            Get["/refresh"] = x =>
             {
                 //return r2wars.Codigo.r2wars.json_output();
                 return "";
             };
            Get["/next"] = x =>
            {
                //r2wars.Codigo.r2wars.btStep_Click();
                //return r2wars.Codigo.r2wars.json_output();
                return "";
            };
            Get["/start"] = x =>
            {
                //r2wars.Codigo.r2wars.btAuto_Click();
                //return "";
                return "";
            };
            Get["/reset"] = x =>
            {
                //r2wars.Codigo.r2wars.btInit_Click();
                //return r2wars.Codigo.r2wars.json_output();
                return "";
            };
        }
    }*/
}
