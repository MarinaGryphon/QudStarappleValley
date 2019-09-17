using System;
using XRL.Rules;
using XRL.UI;
using XRL.Core;
using XRL.World.Parts.Effects;
using System.Collections.Generic;
using System.Text;
using XRL.Liquids;
using System.Linq;
using XRL.World;

namespace XRL.World.Parts
{
	[Serializable]
	public class acegiak_Fermenter : IPart
	{

		public string Result;

        public int wateramount = 0;
        public long growth = 0;
        public int stage = 0;

        public int health = 0;

        public long lastseen = 0;

        public int stageLength = 200;

        public bool Dead = false;

        public string ResultName;

        public string displayname;
        public string description;
        [NonSerialized]
        public static Dictionary<string,string> ferments;



		public acegiak_Fermenter()
		{
		}
		public override bool SameAs(IPart p)
		{
            
			return false;
		}




		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object)
		{
			// Object.RegisterPartEvent(this, "GetInventoryActions");
			// Object.RegisterPartEvent(this, "InvCommandPlant");
            // Object.RegisterPartEvent(this, "ApplyEffect");
            Object.RegisterPartEvent(this, "EndTurn");
            Object.RegisterPartEvent(this, "GetDisplayName");
            Object.RegisterPartEvent(this, "GetShortDisplayName");
            // Object.RegisterPartEvent(this, "GetShortDescription");
            // Object.RegisterPartEvent(this, "AccelerateRipening");
			base.Register(Object);
		}


        public void Water(int drams){
            Ticks();
            this.wateramount += drams;
            //Absorb(drams);

        }

        public LiquidVolume GetPuddle(){
            Cell cell = ParentObject.CurrentCell;
            foreach(GameObject GO in cell.GetObjects()){
                LiquidVolume volume = GO.GetPart<LiquidVolume>();
                if(volume != null){
                    return volume;
                }
            }
            return null;
        }





        public void Ticks(){

            long newGrowth = (XRLCore.Core.Game.TimeTicks - this.lastseen);

            if(this.lastseen == 0){
                newGrowth = 0;
            }
            this.lastseen = XRLCore.Core.Game.TimeTicks;
            this.growth += newGrowth;

            if(this.growth >=stageLength){
            
                for(int i = 0; i < growth/stageLength;i++){
                    //IPart.AddPlayerMessage("TICKS!");

                    Tick();
                }
                this.growth = this.growth % stageLength;

            }
        }

        public void Tick(){

            GetFerments();
            LiquidVolume volume = ParentObject.GetPart<LiquidVolume>();
            if(volume == null){

                return;
            }

            if(ParentObject.pPhysics.Temperature <15){
                return;
            }
            int bads = 0;
            int total = 0;

            
            var test = volume.ComponentLiquids;

            int adram = (int)Math.Ceiling(1000f/Math.Max(volume.Volume,1));


            foreach(byte id in volume._ComponentLiquids.Keys.ToList()){
                if(ferments.ContainsKey(LiquidVolume.ComponentLiquidTypes[id].Name)
                || ferments.Values.ToList().Contains(LiquidVolume.ComponentLiquidTypes[id].Name)){

                }else{
                    bads += volume._ComponentLiquids[id];
                }
                total += volume._ComponentLiquids[id];
            }
            total = Math.Max(total,1);


            if(ParentObject.pPhysics.Temperature > 30f  || Stat.Rnd2.NextDouble() < bads/total){

                byte result = LiquidVolume.GetLiquidId("putrid");
                if(volume.GetPrimaryLiquid() != null){
                    volume._ComponentLiquids[Convert.ToByte(volume.GetPrimaryLiquid().ID)] -= adram;
                }
                volume._ComponentLiquids[result] += adram;
                
            }else{
                foreach(byte id in volume._ComponentLiquids.Keys.ToList()){
                    if(LiquidVolume.ComponentLiquidTypes.ContainsKey(id)){
                        string SourceLiquidName = LiquidVolume.ComponentLiquidTypes[id].Name;
                    
                        if(ferments.ContainsKey(SourceLiquidName)){ //Its a liquid that ferments

                            string ResultName = ferments[SourceLiquidName];


                            if(LiquidVolume.ComponentLiquidNameMap.ContainsKey(SourceLiquidName)){

                                if(LiquidVolume.ComponentLiquidNameMap.ContainsKey(ResultName)){
                                    byte ResultLiquidId = Convert.ToByte(LiquidVolume.ComponentLiquidNameMap[ResultName].ID);

                                    volume._ComponentLiquids[id] = volume._ComponentLiquids[id] - adram;

                                    if(volume._ComponentLiquids[id] <= 0){
                                        volume._ComponentLiquids.Remove(id);
                                    }

                                    if(!volume._ComponentLiquids.ContainsKey(ResultLiquidId)){
                                        volume._ComponentLiquids[ResultLiquidId] = 0;
                                    }

                                    volume._ComponentLiquids[ResultLiquidId] = volume._ComponentLiquids[ResultLiquidId] + adram;

                                }else{
                                    volume._ComponentLiquids[id] = volume._ComponentLiquids[id] - adram;

                                    if(volume._ComponentLiquids[id] <= 0){
                                        volume._ComponentLiquids.Remove(id);
                                    }
                                    volume.Volume -= 1;
                                    ParentObject.GetPart<Inventory>().AddObject(GameObject.create(ResultName));
                                }
                            }
                        }
                    }
                }


                List<string> seen = new List<string>();

                foreach(GameObject GO in ParentObject.GetPart<Inventory>().GetObjects()){

                    if(GO.HasTag("FermentTo")){
                        string result = GO.GetTag("FermentTo");

                        if(!seen.Contains(GO.GetBlueprint().Name)){
                            if(GO.Count > 1){
                                GO.RemoveOne();
                            }else{
                                GO.Destroy();
                            }
                            seen.Add(GO.GetBlueprint().Name);

                            if(LiquidVolume.ComponentLiquidNameMap.ContainsKey(result)){
                                volume.Volume += 1;

                                if(!LiquidVolume.ComponentLiquidNameMap.ContainsKey(result)){
                                    IPart.AddPlayerMessage("ERROR: Tried to ferment "+GO.DisplayName+" but output was invalid:"+result);
                                }else{
                                    Byte LiquidID = Convert.ToByte(LiquidVolume.ComponentLiquidNameMap[result].ID);
                                
                                    if(!volume._ComponentLiquids.ContainsKey(LiquidID)){
                                        volume._ComponentLiquids[LiquidID] = 0;
                                    }

                                    volume._ComponentLiquids[LiquidID] = volume._ComponentLiquids[LiquidID] + adram;
                                }
                            }else{
                                ParentObject.GetPart<Inventory>().AddObject(GameObject.create(result));
                            }
                            
                        }
                    }
                }

            }
            
            volume.NormalizeProportions();
            volume.RecalculatePrimary();
            volume.RecalculateProperties();
            volume.FlushWeightCaches();
       
        }

        

        public static void GetFerments(){

            if(ferments == null){
            ferments = new Dictionary<string,string>();
            List<GameObjectBlueprint> bps = GameObjectFactory.Factory.GetBlueprintsWithTag("FermentTo");
            foreach(GameObjectBlueprint bp in bps){
                if(bp.allparts.ContainsKey("LiquidVolume")){
                    string[] liquidbits = bp.allparts["LiquidVolume"].GetParameter("InitialLiquid").Split('-');
                    //IPart.AddPlayerMessage("Tryadd initliq:"+liquidbits[0]+" / "+String.Join(", ",LiquidVolume.ComponentLiquidNameMap.Keys.ToList().ToArray()));

                    if(LiquidVolume.ComponentLiquidNameMap.ContainsKey(liquidbits[0])){
                        BaseLiquid L = LiquidVolume.ComponentLiquidNameMap[liquidbits[0]];
                        if(LiquidVolume.ComponentLiquidNameMap.ContainsKey(bp.GetTag("FermentTo"))){
                            BaseLiquid F = LiquidVolume.ComponentLiquidNameMap[bp.GetTag("FermentTo")];
                            //ferments[L.Name] = Convert.ToByte(F.ID);
                            IPart.AddPlayerMessage(L.Name+" can ferment to:"+F.Name+"!");
                            ferments[L.Name] = F.Name;
                        }else{
                            ferments[L.Name] = bp.GetTag("FermentTo");
                            IPart.AddPlayerMessage(L.Name+" can ferment to:"+bp.GetTag("FermentTo")+"!");

                        }
                    }
                }else{
                    if(LiquidVolume.ComponentLiquidNameMap.ContainsKey(bp.GetTag("FermentTo"))){
                        BaseLiquid F = LiquidVolume.ComponentLiquidNameMap[bp.GetTag("FermentTo")];
                        //ferments[bp.Name] = Convert.ToByte(F.ID);
                        ferments[bp.Name] = F.Name;
                        IPart.AddPlayerMessage(bp.Name+" can  ferments to:"+F.Name+"!");
                    }else{
                        ferments[bp.Name] = bp.GetTag("FermentTo");
                        IPart.AddPlayerMessage(bp.Name+" can ferment to:"+bp.GetTag("FermentTo")+"!");

                    }
                }
                
            }

            }
            
        }


        
		public override bool FireEvent(Event E)
		{

            if (E.ID == "EndTurn"){
                Ticks();
            }
            // if (E.ID == "GetShortDescription" && this.stage > 0){
            //     string debug = "";
            //     // debug += 
            //     // GetPuddle().GetPrimaryLiquid().GetKeyString()+":"
            //     // +GetPuddle()._ComponentLiquids[GetPuddle.bPrimary].ToString()
            //     // +GetPuddle().GetSecondaryLiquid().GetKeyString()+":"
            //     // +GetPuddle()._ComponentLiquids[GetPuddle.bSecondary].ToString()
            //     // E.SetParameter("ShortDescription", this.description);
            // }
            if (E.ID == "GetDisplayName" || E.ID == "GetShortDisplayName"){
                if(ParentObject.GetPart<Inventory>() != null && ParentObject.GetPart<Inventory>().Objects.Count() >0){
                    int count = 0;
                    foreach(GameObject GO in ParentObject.GetPart<Inventory>().GetObjects()){
                        count += GO.Count;
                    }
       				E.GetParameter<StringBuilder>("Postfix").Append(" &y["+count+" items]");

                }
					
            }
			return base.FireEvent(E);
		}
	}
}
