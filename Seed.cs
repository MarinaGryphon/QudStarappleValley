using System;
using XRL.Rules;
using XRL.UI;
using XRL.Core;
using XRL.World.Parts.Effects;
using System.Collections.Generic;
using System.Text;
using XRL.Liquids;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	[Serializable]
	public class acegiak_Seed : IPart
	{

		public string Result;

        public int wateramount = 0;
        public long growth = 0;
        public int stage = 0;

        public int health = 0;

        public long lastseen = 0;

        public int stageLength = 200;
        public int drowamount = 15;

        public bool Dead = false;

        public string ResultName;

        public string displayname;
        public string description;



		public acegiak_Seed()
		{
		}
        
		public override bool SameAs(IPart p)
		{
            if(p is acegiak_Seed){
                acegiak_Seed s = p as acegiak_Seed;
                if(s.Result == this.Result && s.ResultName == this.ResultName && s.growth == this.growth && s.stage == this.stage){
                    return true;
                }
            }
			return false;
		}




		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterPartEvent(this, "GetInventoryActions");
			Object.RegisterPartEvent(this, "InvCommandPlant");
            Object.RegisterPartEvent(this, "ApplyEffect");
            Object.RegisterPartEvent(this, "EndTurn");
            Object.RegisterPartEvent(this, "GetDisplayName");
            Object.RegisterPartEvent(this, "GetShortDisplayName");
            Object.RegisterPartEvent(this, "GetShortDescription");
            Object.RegisterPartEvent(this, "AccelerateRipening");
			base.Register(Object);
		}

        public void Plant(GameObject who){
            Cell cell = ParentObject.CurrentCell;
            if(cell == null){
                Popup.Show("Put things on the ground to plant them.");
                return;
            }
            if(ParentObject.GetPart<Stacker>() != null && ParentObject.GetPart<Stacker>().StackCount > 1){
                GameObject gameObject = ParentObject.DeepCopy(true);
                gameObject.GetPart<Stacker>().StackCount = ParentObject.GetPart<Stacker>().StackCount -1;
                ParentObject.GetPart<Stacker>().StackCount = 1;
                who.GetPart<Inventory>().AddObject(gameObject);
                IPart.AddPlayerMessage("You plant one "+ParentObject.DisplayNameOnly+" and collect the rest");
            }

            ParentObject.pPhysics.Takeable = false;

            LengthMultiplier();
            this.stage = 1;
            //ParentObject.pPhysics.Category = "Plant";
            //ParentObject.RemovePart<NoEffects>();
            tileupdate();
            // Statistic statistic = new Statistic("Energy", 0, 10000, 0, ParentObject);
            // statistic.Owner = ParentObject;
            // ParentObject.Statistics.Add("Energy", statistic);

            // XRLCore.Core.Game.ActionManager.AddActiveObject(ParentObject);

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

        public void LengthMultiplier(){
            GameObject GO = GameObjectFactory.Factory.CreateSampleObject(this.Result);
            int multiplier = 1;
            if(GO.GetPart<Brain>() != null){
                multiplier++;
            }
            if(GO.GetPart<Body>() != null){
                multiplier++;
            }
            if(GO.GetPart<LiquidFont>() != null){
                multiplier += 2;
            }
            if(GO.GetPart<Harvestable>() != null){
                multiplier++;
            }
            if(GO.GetPart<SporePuffer>() != null){
                multiplier++;
            }
            this.stageLength = this.stageLength*multiplier;
        }




        public void Ticks(){
            if(stage < 1){
                return;
            }
            if(!this.Dead){
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
                    tileupdate();

                }
                
            }




            if(this.stage >=5 && !this.Dead){
                Cell cell = ParentObject.CurrentCell;
                if(cell == null){
                    Popup.Show("Things must grow in the ground.");
                    return;
                }
                GameObject growInto = GameObject.create(Result);



                if(GetPuddle() != null){
                    if(GetPuddle().ComponentLiquids.ContainsKey(Convert.ToByte(acegiak_LiquidRestrainingAgent.ID))){
                       growInto.pPhysics.Takeable = true;
                       growInto.pPhysics.Weight = growInto.pPhysics.Weight /10;
                       growInto.pRender.DisplayName += " bonsai";
                    }
                }

                if(GetPuddle() != null){
                    if(GetPuddle().ComponentLiquids.ContainsKey(Convert.ToByte(acegiak_LiquidFurlingAgent.ID))){
                        GameObject furled = GameObject.create("FurledPlant");
                        furled.GetPart<Render>().DisplayName = "Furled "+growInto.DisplayNameOnly;
                        furled.GetPart<Render>().DetailColor = ParentObject.pRender.DetailColor;
                        furled.GetPart<Render>().TileColor = ParentObject.pRender.TileColor;
                        furled.GetPart<Render>().RenderString = ParentObject.pRender.RenderString;
                        furled.GetPart<Render>().ColorString = ParentObject.pRender.ColorString;
                        furled.GetPart<DeploymentGrenade>().Blueprint = growInto.Blueprint;
                        growInto = furled;
                    }
                }

                



                if(GetPuddle() != null){
                    if(GetPuddle().ComponentLiquids.ContainsKey(Convert.ToByte(acegiak_LiquidSoothingAgent.ID))){
                        if(growInto.GetPart<Brain>() != null){
                            growInto.GetPart<Brain>().PerformReequip();
                            growInto.GetPart<Brain>().BecomeCompanionOf(growInto.ThePlayer);
                            growInto.GetPart<Brain>().IsLedBy(growInto.ThePlayer);
                            growInto.GetPart<Brain>().SetFeeling(growInto.ThePlayer,100);
                            growInto.GetPart<Brain>().Goals.Clear();
                            growInto.GetPart<Brain>().Calm = false;
                            growInto.GetPart<Brain>().Hibernating = false;
                            growInto.GetPart<Brain>().FactionMembership.Clear();
                            growInto.AddPart(new Combat());
                        }
                    }
                }

                if(growInto.GetPart<Brain>() != null){
                            XRLCore.Core.Game.ActionManager.AddActiveObject(growInto);
                }


                cell.AddObject(growInto);


                ParentObject.FireEvent(new Event("acegiak_SeedGrow","From",ParentObject,"To",growInto));
                cell.RemoveObject(ParentObject);
                ParentObject.Destroy(true);
            }
        }

        public void Tick(){
            if(GetPuddle() == null
            || GetPuddle().GetPrimaryLiquid().GetKeyString() != "water"
            || GetPuddle().Volume <= 0
            || GetPuddle().Volume > drowamount){
                health--;
            }else{
                health++;
                if(GetPuddle() != null
                && GetPuddle().ComponentLiquids.ContainsKey(Convert.ToByte(acegiak_LiquidGrowthAgent.ID))
                && GetPuddle().ComponentLiquids[Convert.ToByte(acegiak_LiquidGrowthAgent.ID)]>0){
                    health+= 4;
                    GetPuddle().ComponentLiquids[Convert.ToByte(acegiak_LiquidGrowthAgent.ID)] -= 1;
                }

            }
            if(health >= 5){
                health = 0;
                stage++;
            }
            if(health <= -5){
                this.Dead = true;
            }
            if(GetPuddle() != null){
                GetPuddle().ComponentLiquids[0]--;
                GetPuddle().Volume--;
                if (GetPuddle().Volume <= 0)
                {
                    GetPuddle().Empty();
                }
                else
                {
                    GetPuddle().NormalizeProportions();
                }
                GetPuddle().RecalculatePrimary();
                GetPuddle().RecalculateProperties();
                GetPuddle().FlushWeightCaches();
            }
        }

        public string debugstring(){
            // return "Water:"+wateramount.ToString()+" Health:"+health.ToString()+" Stage:"+stage.ToString()+(Dead?" Dead":(wateramount>7?"Drowning":(wateramount<=2?"Dry":" Alive")))+this.growth.ToString();
            if(Dead){
                return "dead";
            }
            if(GetPuddle() == null){
                return "dry";
            }
            if(GetPuddle().GetPrimaryLiquid().GetKeyString() != "water"){
                return "choking on "+GetPuddle().GetPrimaryLiquid().GetKeyString();
            }
            if(GetPuddle().Volume > drowamount){
                return "drowning";
            }
            if(GetPuddle().Volume <3){
                return "dry";
            }
            
            return "thriving";
        }


        public void tileupdate(){
            if(this.Dead){
                ParentObject.pRender.Tile = "Items/plantedseeddead.png";
                this.displayname = "husk";
                this.description = "This plant has withered and died.";
            }
            if(this.stage == 1){
                ParentObject.pRender.Tile = "Items/plantedseed1.png";
                this.displayname = "seed";
                this.description = "The seed has been planted in the earth.";
            }
            if(this.stage == 2){
                ParentObject.pRender.Tile = "Items/plantedseed2.png";
                this.displayname = "sprout";
                this.description = "It has sprouted from the earth.";
            }
            if(this.stage == 3){
                ParentObject.pRender.Tile = "Items/plantedseed3.png";
                this.displayname = "seedling";
                this.description = "A "+this.ResultName+" sprout has grown into a seedling.";
            }
            if(this.stage == 4){
                ParentObject.pRender.Tile = "Items/plantedseed4.png";
                this.displayname = "plant";
                this.description = "A "+this.ResultName+" plant is growing here.";
            }
        }
		public override bool FireEvent(Event E)
		{
            if (E.ID == "GetInventoryActions")
            {
                if (ParentObject.pPhysics.CurrentCell != null && ParentObject.pPhysics.Takeable)
                {
                    E.GetParameter<EventParameterGetInventoryActions>("Actions").AddAction("Plant", 'P', false, "&WP&ylant", "InvCommandPlant", 5);
                }
            }
            else if (E.ID == "InvCommandPlant")
            {
                Plant(E.GetParameter<GameObject>("Owner"));
                        E.RequestInterfaceExit();
            }else
            // if(E.ID == "ApplyEffect"){

            //     Popup.Show("'e got an effect");
            //     Effect effect = E.GetParameter("Effect") as Effect;     
            //     if(effect != null && effect is LiquidCovered){
            //         Popup.Show("'e got liquidcovered:");

            //         LiquidVolume volume = ((LiquidCovered)effect).Liquid;
            //         if(volume.GetLiquidName().Contains("fresh water")){
            //              Popup.Show("'e got wet:"+((LiquidCovered)effect).ContactDrams);
            //             Water(((LiquidCovered)effect).ContactDrams);
            //             volume.Volume = 0;
            //         }
            //     }
            // }
            if (E.ID == "EndTurn" || E.ID == "AccelerateRipening"){
                Ticks();
            }
            if (E.ID == "GetShortDescription" && this.stage > 0){
                string debug = "";
                // debug += 
                // GetPuddle().GetPrimaryLiquid().GetKeyString()+":"
                // +GetPuddle().ComponentLiquids[GetPuddle.bPrimary].ToString()
                // +GetPuddle().GetSecondaryLiquid().GetKeyString()+":"
                // +GetPuddle().ComponentLiquids[GetPuddle.bSecondary].ToString()
                // E.SetParameter("ShortDescription", this.description);
            }
            if (E.ID == "GetDisplayName" || E.ID == "GetShortDisplayName"){
                 if(this.stage > 0){
					 E.AddParameter("DisplayName",new StringBuilder(this.ResultName+" "+this.displayname+ " &y["+debugstring()+"]"));
                }
					
            }
			return base.FireEvent(E);
		}
	}
}
