using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace r2warsTorneo
{
    public class clsinfo
    {
        public string pc = "";
        public string ins = "";
        public string dasm = "";
        public string regs = "";
        public string mem = "";
        public string txtmemoria = "";
        public int cycles = 0;
        public bool dead = false;

        public clsinfo(string pc, string ins, string dasm, string regs, string mem, int cycles, string txtmemoria)
        {
            this.pc = pc;
            this.ins = ins;
            this.dasm = dasm;
            this.regs = regs;
            this.mem = mem;
            this.cycles = cycles;
            this.txtmemoria = txtmemoria;
        }
        public int ipc()
        {
            if (pc != "")
            {
                int ipc = Convert.ToInt32(pc.Substring(3), 16);
                return ipc;
            }
            return -1;
        }
        public string formatregs()
        {
            string[] reg = regs.Split('\n');
            string regformat = "";
            int x = 0;
            foreach (string s in reg)
            {
                if (s.StartsWith("oeax") == false)
                {
                    if (x % 3 == 0 && x != 0)
                        regformat += "\n";
                    regformat += s + " ";
                    x++;
                }
            }
            return regformat;
        }
    }
    public class player
    {
        public string name = "";
        public int orig = 0;
        public int size = 0;
        public string user = "";
        public string code = "";
        public string userini = "";
        public int cycles = 0;
        public int cyclesfixed = 0;
        public clsinfo actual;

        public List<clsinfo> log;
        int idxlog;
        public player(string name, int orig, int size, string code, string user, int ciclos) //,string dasm,string mem,string ins, string pc, string reg)
        {
            this.name = name;
            this.orig = orig;
            this.size = size;
            this.user = user;
            this.code = code;
            this.userini = user;
            this.cycles = ciclos;
            this.cyclesfixed = ciclos;
            this.log = new List<clsinfo>();
            this.actual = new clsinfo("", "", "", "", "", 0,"");
            idxlog = -1;
        }


        public void logClear()
        {
            log.Clear();
            idxlog = 0;
        }
        public void logAdd(clsinfo entry)
        {
            log.Add(entry);
            idxlog = log.Count();
        }
        public string logGetFull()
        {
            string res = "";
            for (int x = 0; x < log.Count; x++)
            {
                res += log[x].formatregs() + "\n" + log[x].dasm + "\n";
            }
            return res;
        }
        public clsinfo logGetPrev()
        {
            if (idxlog <0)
            {
                return null;
            }
            else if (idxlog > 0)
            {
                idxlog--;
            }
            return log[idxlog];
        }
        public clsinfo logGetNext()
        {
            if (idxlog < 0 || idxlog>log.Count-1)
            {
                return null;
            }
            else if (idxlog < log.Count - 1)
            {
                idxlog++;
            }
            return log[idxlog];
        }
    }
   
    public class clsEngine
    {
        private string _r2path = "";
        private string _rasm2path = "";
        public int memsize = 1024;
        public int maxprogsize = 64;
        private IR2Pipe r2 = null;
        private int nPlayers = 0;
        public int uidx = 0;
        public List<player> players = new List<player>();
        public enum eArch
        {
            mips32,
            mips64,
            arm16,
            arm32,
            arm64,
            gb,
            x86,
            x64
        }
        private eArch _arch = eArch.x86;
        public string arch
        {
            get
            {
                if (_arch == eArch.arm16)
                    return "arm 16 bits";
                if (_arch == eArch.arm32)
                    return "arm 32 bits";
                if (_arch == eArch.arm64)
                    return "arm 64 bits";
                if (_arch == eArch.mips32)
                    return "mips 32 bits";
                if (_arch == eArch.mips64)
                    return "mips 64bits";
                if (_arch == eArch.gb)
                    return "gb 16 bits";
                if (_arch == eArch.x64)
                    return "x86 64 bits";
                return "x86 32 bits";
            }
        }
        public string r2path
        {
            get
            {
                
                if (_r2path == "")
                {
                    string ruta = "";
                    if (!OperatingSystem.IsWindows())
                        ruta = "r2";
                    else
                        ruta = "radare2.exe";
                    return ruta;
                }
                return this._r2path;
              
            }
            set
            {
                this._r2path = value;
            }
        }
        public string rasm2path
        {
            get
            {

                if (_rasm2path == "")
                {
                    string ruta = "";
                    if (!OperatingSystem.IsWindows())
                        ruta = "rasm2";
                    else
                        ruta = "rasm2.exe";
                    return ruta;
                }
                else
                    return this._rasm2path;

            }
            set
            {
                this._rasm2path = value;
            }
        }
        public string runcommand(string cmd)
        {
            return this.r2.RunCommand(cmd).Replace("\r", "");
        }
        public string RunAndGetOutput(string program, string arguments)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = program;
            p.StartInfo.Arguments = arguments;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

        List<int> get_random_offsets(int nRandoms)
        {
            System.Random rnd = new System.Random();
            List<int> rand = new List<int>();
            int addr = rnd.Next(0, this.memsize - this.maxprogsize);
            if (addr % 4 != 0)
                addr -= addr % 4;
            rand.Add(addr);
            int ini = addr;
            int fin = addr + maxprogsize;
            while (true)
            {
                addr = rnd.Next(0, this.memsize - this.maxprogsize);
                if (addr % 4 != 0)
                    addr -= addr % 4;
                if ((addr < ini && addr + maxprogsize < ini) || (addr > fin))
                {
                    rand.Add(addr);
                    break;
                }
            }
            return rand;
        }
        string initstate = "";
        public string Init(string[] files, string[] usernames, eArch arch)
        {
            players.Clear();
            initstate = "";
            Console.WriteLine("r2path =  " + r2path);
            if (File.Exists(r2path) || !OperatingSystem.IsWindows())
            {
                List<int> offsets;
                int idx = 0;
                string file = "";
                string strname = "";
                string cone = string.Format("-w malloc://{0}", this.memsize);
                if (this.r2 == null)
                    this.r2 = new R2Pipe(cone, r2path);
                // el primer comando enviado no parece funcionar luego todo va bien.
                this.r2.RunCommand("pd");
                //
                string rasm2param = "";
                if (arch == eArch.mips32)
                {
                    _arch = eArch.mips32;
                    this.r2.RunCommand("e asm.arch=mips");
                    this.r2.RunCommand("e asm.bits=32");
                    rasm2param = "-a mips -b 32";
                }
                else if (arch == eArch.mips64)
                {
                    _arch = eArch.mips32;
                    this.r2.RunCommand("e asm.arch=mips");
                    this.r2.RunCommand("e asm.bits=64");
                    rasm2param = "-a mips -b 64";
                }
                else if (arch == eArch.arm64)
                {
                    _arch = eArch.mips32;
                    this.r2.RunCommand("e asm.arch=arm");
                    this.r2.RunCommand("e asm.bits=64");
                    rasm2param = "-a mips -b 64";
                }
                else if (arch == eArch.arm32)
                {
                    _arch = eArch.arm32;
                    this.r2.RunCommand("e asm.arch=arm");
                    this.r2.RunCommand("e asm.bits=32");
                    rasm2param = "-a arm -b 32";
                }
                else if (arch == eArch.arm16)
                {
                    _arch = eArch.arm16;
                    this.r2.RunCommand("e asm.arch=arm");
                    this.r2.RunCommand("e asm.bits=16");
                    rasm2param = "-a arm -b 16";
                }
                else if (arch == eArch.gb)
                {
                    _arch = eArch.gb;
                    this.r2.RunCommand("e asm.arch=gb");
                    this.r2.RunCommand("e asm.bits=16");
                    rasm2param = "-a gb -b 16";
                }
                else if (arch ==  eArch.x64)
                {
                    _arch = eArch.x64;
                    this.r2.RunCommand("e asm.arch=x86");
                    this.r2.RunCommand("e asm.bits=64");
                    rasm2param = "-a x86 -b 64";
                }
                else
                {
                    _arch = eArch.x86;
                    this.r2.RunCommand("e asm.arch=x86");
                    this.r2.RunCommand("e asm.bits=32");
                    rasm2param = "-a x86 -b 32";
                }
                this.r2.RunCommand("e scr.color=false");
                this.r2.RunCommand("e asm.lines=false");
                this.r2.RunCommand("e asm.flags=false");
                this.r2.RunCommand("e asm.comments=false");
                this.r2.RunCommand("aei");
                
                if (initstate == "")
                {
                    this.r2.RunCommand("aeim");

                    initstate = this.r2.RunCommand("aerR").Replace("\r", "").Replace("\n", ";");
                }
                else
                {
                    this.r2.RunCommand(initstate);
                }

                this.r2.RunCommand(string.Format("w0 {0} @ 0", memsize));

                nPlayers = files.Count();
                offsets = get_random_offsets(this.nPlayers);
                for (int x = 0; x < this.nPlayers; x++)
                {
                    file = files[x];
                    int addr = offsets[idx];
                    idx++;
                    string src = RunAndGetOutput(rasm2path, string.Format("{0} -f {1}", rasm2param, file)).Replace("\r", "").Replace("\n", "");
                    if (src == "")
                    {
                        return "Invalid source";
                    }
                    Console.WriteLine("Rasm Output =  " + src);
                    // Añadimos el codigo a radare2
                    string cmd = string.Format("wx {0} @ {1}", src, addr);
                    this.r2.RunCommand(cmd);
                    cmd = string.Format("aer PC={0}", addr);
                    this.r2.RunCommand(cmd);
                    cmd = string.Format("aer SP=SP+{0}", addr);
                    this.r2.RunCommand(cmd);

                    string initRegs = this.r2.RunCommand("aerR").Replace("\r", "").Replace("\n", ";");
                    // generamos el jugador
                    strname = usernames[x];
                    int ciclos = GetPCInstructionCycles(x);
                    // Iniciamos el jugador añadiendo sus datos de programa, ciclos
                    player p = new player(strname, addr, src.Length / 2, src, initRegs, ciclos);
                    // lo añadimos a la lista de jugadores
                    players.Add(p);

                    players[x].actual.ins = GetPCInstruction(x);
                    players[x].actual.dasm = GetFullProgram(x);
                    players[x].actual.regs = GetRegs(x);
                    players[x].actual.pc = "0x" + GetPC(x).ToString("X8");
                    players[x].actual.mem = GetMemAccessRAW(x);
                    // borramos el log
                    p.log.Clear();
                 

                }
                if (players.Count < 2)
                {
                    return "You need at least 2 users";
                }
                this.r2.RunCommand("e cmd.esil.todo=f theend=1");
                this.r2.RunCommand("e cmd.esil.trap=f theend=1");
                this.r2.RunCommand("e cmd.esil.intr=f theend=1");
                this.r2.RunCommand("e cmd.esil.ioer=f theend=1");
                this.r2.RunCommand("f theend=0");
                this.r2.RunCommand(string.Format("b {0}", memsize));
                return "OK";
            }
            return "NOK";
        }

        public bool ReiniciaGame(bool bNew)
        {
            List<int> offsets = get_random_offsets(this.nPlayers);
            string cmd = "";
            this.r2.RunCommand(string.Format("w0 {0} @ 0", memsize));
            for (int x = 0; x < this.nPlayers; x++)
            {
                this.r2.RunCommand(players[x].userini);
                players[x].user = players[x].userini;
                if (bNew)
                {
                    // ponemos la nueva direccion
                    players[x].orig = offsets[x];
                    // restauramos el contexto inicial
                    this.r2.RunCommand(players[x].user);
                    // ponemos el nuevo PC
                    cmd = string.Format("aer PC={0}", offsets[x]);
                    this.r2.RunCommand(cmd);
                    // obtenemos los registros
                    string initRegs = this.r2.RunCommand("aerR").Replace("\r", "").Replace("\n", ";");
                    // actualizamos los registros iniciales del usuario
                    players[x].user = initRegs;
                    players[x].userini = initRegs;
                }
                else
                {
                    // restauramos el contexto inicial
                    this.r2.RunCommand(players[x].user);
                }
                cmd = string.Format("wx {0} @ {1}", players[x].code, players[x].orig);
                this.r2.RunCommand(cmd);
                // Actualizamos la informacion del PC actual
                players[x].actual.ins = GetPCInstruction();
                players[x].actual.dasm = GetFullProgram();
                players[x].actual.regs = GetRegs();
                players[x].actual.pc = "0x" + GetPC().ToString("X8");
                players[x].actual.mem = GetMemAccessRAW();
                // borramos el log
                players[x].log.Clear();
            }

            this.r2.RunCommand("f theend=0");
            return true;
        }
        public bool checkDead()
        {
            string te = this.r2.RunCommand("?v 1+theend").Replace("\r", "").Replace("\n", "");
            if (te != "" && te != "0x1")
            {
                return true;
            }
            return false;
        }
        public void switchUser()
        {
            uidx++;
            if (uidx >= players.Count)
                uidx = 0;
            this.r2.RunCommand(players[this.uidx].user);
        }
        public void switchUser(int nuser)
        {
            if (nuser >= 0 && nuser < players.Count)
            {
                this.r2.RunCommand(players[nuser].user);
                uidx = nuser;
            }
        }
        public int thisplayer
        {
            get
            {
                return this.uidx;
            }
        }
        
        public int otherplayer 
        {
            get
            {
                int o = this.uidx + 1;
                if (o >= players.Count)
                    o = 0;
                return o;
            }
        }
        public void switchUserIdx()
        {
            uidx++;
            if (uidx >= players.Count)
                uidx = 0;
        }
        public void switchUserIdx(int nuser)
        {
            if (nuser >= 0 && nuser < players.Count)
            {
                uidx = nuser;
            }
        }

        public bool cyleszero()
        {
        
            if (players[this.uidx].cycles == 0)
            {
                players[this.uidx].cycles = -1;
                return true;
            }
            players[this.uidx].cycles--;
            return false;
        }
        public int stepIn()
        {
            this.r2.RunCommand("aes");
            players[this.uidx].user = this.r2.RunCommand("aerR").Replace("\r", "").Replace("\n", ";");
            players[this.uidx].cycles = GetPCInstructionCycles();
            return GetPC(this.uidx);
        }

        public void stepIn(int nuser)
        {
            switchUser(nuser);
            stepIn();
        }
        public int GetPC()
        {
            string tmp = this.r2.RunCommand("aer PC");
            string tmp1 = tmp.Substring(3).Replace("\r", "").Replace("\n", "");
            int res = Convert.ToInt32(tmp1, 16);
            return res;
        }
        public int GetPC(int nuser)
        {
            switchUser(nuser);
            return GetPC();
        }
        public string GetPCInstruction()
        {
            string a = this.r2.RunCommand("o");
            string b = this.r2.RunCommand("s PC;pd 1").Replace("\r", "");
            return b.Replace("\n","");
        }
        public string GetPCInstruction(int nuser)
        {
            switchUser(nuser);
            return GetPCInstruction();
        }

        public int GetPCInstructionCycles(int nuser)
        {
            switchUser(nuser);
            return GetPCInstructionCycles();
        }
        public int GetPCInstructionCycles()
        {
            int result = 0;
            string json = this.r2.RunCommand("s PC; aoj 1").Replace("\r", "");
            int ini = json.IndexOf("cycles");
            if (ini > 0)
            {
                string tmp = json.Substring(ini + 8, json.IndexOf(",", ini + 8) - (ini + 8));
                int.TryParse(tmp, out result);
                if (result < 0) result = 0;
            }
            return result;
        }


        public string GetRegs(string format = "")
        {
            return this.r2.RunCommand("aer" + format).Replace("\r", "");
        }
        public string GetRegs(int nuser)
        {
            switchUser(nuser);
            return GetRegs();
        }
        public string GetPCProgram()
        {
            string query = string.Format("s PC; pd 8");
            string tmp = this.r2.RunCommand(query);
            return tmp.Replace("\r", "");
        }
        public string GetPCProgram(int nuser)
        {
            switchUser(nuser);
            return GetPCProgram();
        }

        public string GetFullProgram()
        {
            string query = string.Format("pD {0} @ {1}", players[this.uidx].size, players[this.uidx].orig);
            string tmp = this.r2.RunCommand(query);
            return tmp.Replace("\r", "");
        }
        public string GetFullProgram(int nuser)
        {
            switchUser(nuser);
            return GetFullProgram();
        }

        public string GetMemAccessRAW()
        {
            return this.r2.RunCommand("s PC;aea*").Replace("\r", "");
        }
        public string GetMemAccessRAW(int nuser)
        {
            switchUser(nuser);
            return GetMemAccessRAW();
        }
        public Dictionary<int, int> GetMemAccessReadDict(string memaccess)
        {
            string[] ls = memaccess.Split('\n');
            int address = 0;
            int size = 0;
            Dictionary<int, int> dicMemRead = new Dictionary<int, int>();
            foreach (string s in ls)
            {
                if (s.StartsWith("f mem.read."))
                {
                    try
                    {
                        string[] l = s.Substring(s.IndexOf("0x")).Replace(" ", "").Split('@');
                        size = Convert.ToInt32(l[0].Substring(2), 16);
                        address = Convert.ToInt32(l[1].Substring(2), 16);
                        dicMemRead.Add(address, size);
                    }
                    catch (Exception ex)
                    {
                       
                    }
                }
            }
            return dicMemRead;
        }
        public int GetMemAccessRead(string memaccess)
        {
            Dictionary<int, int> dicMemRead = GetMemAccessReadDict(memaccess);
            if (dicMemRead.Count > 0)
            {
                foreach (var a in dicMemRead)
                {
                    if (a.Key >= 0 && a.Key <= memsize)
                        return a.Key;
                    else
                        return -1;
                }
            }
            return -1;
        }
        public int GetMemAccessRead(int nuser, string memaccess)
        {
            switchUser(nuser);
            return GetMemAccessRead(memaccess);
        }

        public Dictionary<int, int> GetMemAccessWriteDict(string memaccess)
        {
            string[] ls = memaccess.Split('\n');
            int address = 0;
            int size = 0;
            Dictionary<int, int> dicMemWrite = new Dictionary<int, int>();
            foreach (string s in ls)
            {
                if (s.StartsWith("f mem.write."))
                {
                    try
                    {
                        string[] l = s.Substring(s.IndexOf("0x")).Replace(" ", "").Split('@');
                        size = Convert.ToInt32(l[0].Substring(2), 16);
                        address = Convert.ToInt32(l[1].Substring(2), 16); 
                        dicMemWrite.Add(address, size);
                    }
                    catch (Exception ex)
                    {

                    }
                }

            }
            return dicMemWrite;
        }
        public int GetMemAccessWrite(string memaccess)
        {
            Dictionary<int, int> dicMemWrite = GetMemAccessWriteDict(memaccess);
            if (dicMemWrite.Count > 0)
            {
                foreach (var a in dicMemWrite)
                {
                    if (a.Key >= 0 && a.Key <= memsize)
                        return a.Key;
                    else
                        return -1;
                }
            }
            return -1;
        }
        public int GetMemAccessWrite(int nuser, string memaccess)
        {
            switchUser(nuser);
            return GetMemAccessWrite(memaccess);
        }

        public string GetUserName()
        {
            return players[uidx].name;
        }
        public string GetUserName(int nuser)
        {
            if (players.Count > 0)
                return players[nuser].name;
            else
                return "";
        }

        public int GetAddressProgram()
        {
            return players[uidx].orig;
        }
        public int GetAddressProgram(int nuser)
        {
            return players[nuser].orig;
        }

        public int GetSizeProgram()
        {
            return players[uidx].size;
        }
        public int GetSizeProgram(int nuser)
        {
            return players[nuser].size;
        }
        public int GetCycles()
        {
            return players[uidx].cycles;
        }
        public int GetCycles(int nuser)
        {
            return players[nuser].cycles;
        }

    

        public bool stepInfoNew(string txtmemoria)
        {
            // Obtenemos los indices del jugador actual y el otro
            int thisplayer = uidx;
            int otherplayer = uidx + 1;
            if (otherplayer >= players.Count)
                otherplayer = 0;

            string res = this.r2.RunCommand(players[thisplayer].user + ";s PC;aea*;aes;aerR;s PC;aoj 1;aer PC;?v 1+theend").Replace("\r", "");
            string[] lines = res.Split('\n');
            string[] regs = lines.Where(i => i.StartsWith("aer")).ToArray();
            string[] memaccess = lines.Where(i => i.StartsWith("f")).ToArray();
            string[] json = lines.Where(i => i.StartsWith("[")).ToArray();
            string[] PC = lines.Where(i => i.StartsWith("0x")).ToArray();

            string userregs = string.Join(";", regs);
            string regs1 = string.Join("\n", regs).Replace("aer ", "");
            string mem = string.Join("\n", memaccess);
            string j = json[0];
            int ini = j.IndexOf("cycles");
            int ciclos = 0;
            if (ini > 0)
            {
                string tmp = j.Substring(ini + 8, j.IndexOf(",", ini + 8) - (ini + 8));
                int.TryParse(tmp, out ciclos);
                if (ciclos < 0) ciclos = 0;
            }
            
            // Generamos la peticion de desensamblado del PC actual del jugador actual
            int pc = Convert.ToInt32(PC[0].Substring(3), 16);
            string query = "";
            if (pc < players[thisplayer].orig || pc >= players[thisplayer].orig + players[thisplayer].size)
                query = string.Format("s PC; pd 8");
            else
                query = string.Format("pD {0} @ {1}", players[thisplayer].size, players[thisplayer].orig);

            
            int otherpc = players[otherplayer].actual.ipc();
            Dictionary<int, int> dicMemWrite = GetMemAccessWriteDict(mem);
            bool bThisPCAltered = false;
            bool bOtherPCAltered = false;

            if (dicMemWrite.Count > 0)
            {
                foreach (var i in dicMemWrite)
                {
                    if (i.Key >= 0 && i.Key <= memsize)
                    {
                        for (int x = 0; x < i.Value; x++)
                        {
                            if (i.Key + x == otherpc)
                            {
                                bOtherPCAltered = true;
                            }
                            if (i.Key + x == pc)
                            {
                                bThisPCAltered = true;
                            }

                        }
                    }
                }
            }

            // Generamos la peticion de desensamblado del PC del otro jugador
            string otherquery = "";
            if (otherpc < players[otherplayer].orig || otherpc >= players[otherplayer].orig + players[otherplayer].size)
                otherquery = string.Format("s {0}; pd 8", players[otherplayer].actual.pc);
            else
                otherquery = string.Format("pD {0} @ {1}", players[otherplayer].size, players[otherplayer].orig);

            // Procesamos el output de radare y obtenemos los 2 desensamblados
            string bothdasm = this.r2.RunCommand(query + ";?e split;" + otherquery).Replace("\r", "");
            string[] tmpdasm = bothdasm.Split(new string[] { "split" }, StringSplitOptions.None);
            string dasm = tmpdasm[0];
            string otherdasm = tmpdasm[1];

            int x1 = dasm.IndexOf(PC[0]);
            string newins = dasm.Substring(x1, dasm.IndexOf('\n', x1) - x1);


            if (PC[1] != "" && PC[1] != "0x1")
                players[thisplayer].actual.dead = true;
            else
                players[thisplayer].actual.dead = false;

            // si han cambiado los desensamblados del otro jugador, actualizamos y añadimos una linea de log con el nuevo.
            if (bOtherPCAltered)
            {
                players[otherplayer].actual.dasm = otherdasm;
            }
            //
            // si han cambiado los desensamblados del jugador actual, actualizamos y añadimos una linea de log con el nuevo.
            if (bThisPCAltered)
            {
                players[thisplayer].actual.dasm = dasm;
            }

            // Añadimos al log la instruccion que se ejecuto
            players[thisplayer].logAdd(new clsinfo(players[thisplayer].actual.pc, players[thisplayer].actual.ins, players[thisplayer].actual.dasm, players[thisplayer].actual.regs, players[thisplayer].actual.mem, players[thisplayer].cycles+1, txtmemoria));


            // Actualizamos los ciclos y registros 
            players[this.uidx].user = userregs;
           
            if (players[this.uidx].cycles == -1)
                players[this.uidx].cyclesfixed = ciclos;

            players[this.uidx].cycles = ciclos;

          
            // Actualizamos la instruccion actual
            players[thisplayer].actual.pc = PC[0];
            players[thisplayer].actual.ins = newins;
            players[thisplayer].actual.mem = mem;
            players[thisplayer].actual.dasm = dasm;
            players[thisplayer].actual.regs = regs1;

        
            return players[thisplayer].actual.dead;
        }

        public string formatregs(string regs)
        {
            string[] reg = regs.Split('\n');
            string regformat = "";
            int x = 0;
            foreach (string s in reg)
            {
                if (s.StartsWith("oeax") == false)
                {
                    if (x % 3 == 0 && x != 0)
                        regformat += "\n";
                    regformat += s + " ";
                    x++;
                }
            }
            return regformat;
        }
    }



}
