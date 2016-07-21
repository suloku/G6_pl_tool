/*
 * Created by SharpDevelop.
 * User: suloku
 * Date: 18/10/2015
 * Time: 9:17
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace XYORAS_Pokemon_Link_Tool
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		int game = 0; //1 XY, 2 ORAS
		string linkfile;
		byte[] savebuffer_XY = new byte[0x65600];
		byte[] savebuffer_ORAS = new byte[0x76000];
		byte[] linkbuffer = new byte[2631];
		PL6 pokemonlink;

		//adapted from Gocario's PHBank (www.github.com/gocario/phbank)
		byte[] ccitt16(byte[] data)
		// --------------------------------------------------
		{
			int len = data.Length;
			UInt16 crc = 0xFFFF;
		
			for (UInt32 i = 0; i < len; i++)
			{
				crc ^= ((UInt16)((data[i] << 8)&0x0000FFFF));
		
				for (UInt32 j = 0; j < 0x8; j++)
				{
					if ((crc & 0x8000) > 0)
						crc = (UInt16)(((UInt16)((crc << 1)&0x0000FFFF ) ^ 0x1021) &0x0000FFFF);
					else
						crc <<= 1;
				}
			}
		
			return BitConverter.GetBytes(crc);
		}

		/// <summary>
		/// Reads data into a complete array, throwing an EndOfStreamException
		/// if the stream runs out of data first, or if an IOException
		/// naturally occurs.
		/// </summary>
		/// <param name="stream">The stream to read data from</param>
		/// <param name="data">The array to read bytes into. The array
		/// will be completely filled from the stream, so an appropriate
		/// size must be given.</param>
		public static void ReadWholeArray (Stream stream, byte[] data)
		{
		    int offset=0;
		    int remaining = data.Length;
		    while (remaining > 0)
		    {
		        int read = stream.Read(data, offset, remaining);
		        if (read <= 0)
		            throw new EndOfStreamException 
		                (String.Format("End of stream reached with {0} bytes left to read", remaining));
		        remaining -= read;
		        offset += read;
		    }
		}
		private void Read_data()
		{
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(savegamename.Text, FileMode.Open);
	            if (saveFile.Length != 0x65600 && saveFile.Length != 0x76000 ){
	            	savegamename.Text = "";
	            	MessageBox.Show("Invalid file length", "Error");
	            	return;
	            }
	            if (saveFile.Length == 0x65600){
	            	game = 1;
	            	currgame.Text = "X/Y";
		            ReadWholeArray(saveFile, savebuffer_XY);
		            //Now get link data
            		savebuffer_XY.Skip(0x1FFFF).Take(0xA47).ToArray().CopyTo(linkbuffer, 0);
		            pokemonlink = new PL6(linkbuffer);
		            saveFile.Close();
	            }else if (saveFile.Length == 0x76000){
	            	game = 2;
	            	currgame.Text = "OR/AS";
		            ReadWholeArray(saveFile, savebuffer_ORAS);
					savebuffer_ORAS.Skip(0x20FFF).Take(0xA47).ToArray().CopyTo(linkbuffer, 0);
		            pokemonlink = new PL6(linkbuffer);
		            saveFile.Close();
	            }
		}
		private void Get_save_data()
        {
            OpenFileDialog openFD = new OpenFileDialog();
            //openFD.InitialDirectory = "c:\\";
            openFD.Filter = "VI gen save data|main|All Files (*.*)|*.*";
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                #region filename
                savegamename.Text = openFD.FileName;
                #endregion
                Read_data();
                
                //Load pkmlink to editor
                linkedit_load();
            }
            
        }
		private void Save_data()
		{	if (savegamename.Text.Length < 1) return;
            SaveFileDialog saveFD = new SaveFileDialog();
            //saveFD.InitialDirectory = "c:\\";
            saveFD.Filter = "VI gen save data|main|All Files (*.*)|*.*";
            if (saveFD.ShowDialog() == DialogResult.OK)
            {
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(saveFD.FileName, FileMode.Create);            
	            //Write file
	            if (game == 1){
	            	saveFile.Write(savebuffer_XY, 0, savebuffer_XY.Length);
	            }else if (game == 2){
	            	saveFile.Write(savebuffer_ORAS, 0, savebuffer_ORAS.Length);
	            }
	            saveFile.Close();
	            MessageBox.Show("File Saved.", "Save file");
            }
		}
		private void Dump_link_data()
		{	if (savegamename.Text.Length < 1) return;
            SaveFileDialog saveFD = new SaveFileDialog();
            //saveFD.InitialDirectory = "c:\\";
            saveFD.Filter = "Pokémon Link Data|*.bin|All Files (*.*)|*.*";
            if (saveFD.ShowDialog() == DialogResult.OK)
            {
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(saveFD.FileName, FileMode.Create);            
	            //Write file
	            if (game == 1){
	            	saveFile.Write(savebuffer_XY, 0x1FFFF, 0xA47);
	            }else if (game == 2) {
	            	saveFile.Write(savebuffer_ORAS, 0x20FFF, 0xA47);
	            }
	            
	            saveFile.Close();
	            MessageBox.Show("Pokémon Link data dumped to:\r"+saveFD.FileName+".", "Dump Entralink Forest Data");
            }
		}
		private void Read_link_data()
		{
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(linkfile, FileMode.Open);
	            if (saveFile.Length != 0xA47){
	            	//linkfile = "";
	            	MessageBox.Show("Invalid file length", "Error");
	            	return;
	            }
	            ReadWholeArray(saveFile, linkbuffer);
	            saveFile.Close();
	            InjectNsave();
		}
		private void Get_link_data()
        {
            OpenFileDialog openFD = new OpenFileDialog();
            //openFD.InitialDirectory = "c:\\";
            openFD.Filter = "Pokémon Link Data|*.bin|All Files (*.*)|*.*";
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                #region filename
                linkfile = openFD.FileName;
                #endregion
                Read_link_data();
            }
            
        }
		private void InjectNsave()
		{

			if (game == 1){
				//Put link data in save
				Array.Copy(linkbuffer, 0, savebuffer_XY, 0x1FFFF, 0xA47);
				//Get full block for checksum calculation
				byte[] giftblockbuffer = new byte[0xC48];
				Array.Copy(savebuffer_XY, 0x1FE00, giftblockbuffer, 0, 0xC48);
				byte[] tablecrcsum = new byte[2];
				tablecrcsum = ccitt16(giftblockbuffer);
				//Put new checksum in savefile
				Array.Copy(tablecrcsum, 0, savebuffer_XY, 0x6559A, 2);
			}
			else if (game == 2){
				//Put link data in save
				Array.Copy(linkbuffer, 0, savebuffer_ORAS, 0x20FFF, 0xA47);
				//Get full block for checksum calculation
				byte[] giftblockbuffer = new byte[0xC48];
				Array.Copy(savebuffer_ORAS, 0x20E00, giftblockbuffer, 0, 0xC48);
				byte[] tablecrcsum = new byte[2];
				tablecrcsum = ccitt16(giftblockbuffer);
				//Put new checksum in savefile
				Array.Copy(tablecrcsum, 0, savebuffer_ORAS, 0x75F9A, 2);
			}
			//Write Data
			Save_data();
		}
		void LoadsaveClick(object sender, EventArgs e)
		{
			Get_save_data();
		}
		void Dump_butClick(object sender, EventArgs e)
		{
			if (savegamename.Text.Length < 1) return;
			Dump_link_data();
		}
		void SavegamenameTextChanged(object sender, EventArgs e)
		{
			if (savegamename.Text.Length > 0){
				dump_but.Enabled = true;
				inject_but.Enabled = true;
			}else{
				dump_but.Enabled = false;
				inject_but.Enabled = false;
			}

		}
		void Inject_butClick(object sender, EventArgs e)
		{
			Get_link_data();
		}

		
		void linkedit_load()
		{
			app.Text = pokemonlink.Origin_app;
			PKL_enabled.Checked = pokemonlink.PL_enabled;
			
			battlepoints.Value = pokemonlink.BattlePoints;
			pokemiles.Value = pokemonlink.Pokemiles;
			
			//Items
			item1.SelectedIndex = pokemonlink.Item_1;
			item2.SelectedIndex = pokemonlink.Item_2;
			item3.SelectedIndex = pokemonlink.Item_3;
			item4.SelectedIndex = pokemonlink.Item_4;
			item5.SelectedIndex = pokemonlink.Item_5;
			item6.SelectedIndex = pokemonlink.Item_6;
			
			item1_cuant.Value = pokemonlink.Quantity_1;
			item2_cuant.Value = pokemonlink.Quantity_2;
			item3_cuant.Value = pokemonlink.Quantity_3;
			item4_cuant.Value = pokemonlink.Quantity_4;
			item5_cuant.Value = pokemonlink.Quantity_5;
			item6_cuant.Value = pokemonlink.Quantity_6;
			
			//Pokemon slots
			slot1.Text = species.GetItemText(species.Items[pokemonlink.Pokes[0].Species]);
			slot2.Text = species.GetItemText(species.Items[pokemonlink.Pokes[1].Species]);
			slot3.Text = species.GetItemText(species.Items[pokemonlink.Pokes[2].Species]);
			slot4.Text = species.GetItemText(species.Items[pokemonlink.Pokes[3].Species]);
			slot5.Text = species.GetItemText(species.Items[pokemonlink.Pokes[4].Species]);
			slot6.Text = species.GetItemText(species.Items[pokemonlink.Pokes[5].Species]);

		
			load_pkm(0);
			slot1.BackColor = Color.LightGreen;
			slot2.BackColor = Color.LightGray;
			slot3.BackColor = Color.LightGray;
			slot4.BackColor = Color.LightGray;
			slot5.BackColor = Color.LightGray;
			slot6.BackColor = Color.LightGray;
			
			pkm_load1.Enabled = true;
			pkm_load2.Enabled = true;
			pkm_load3.Enabled = true;
			pkm_load4.Enabled = true;
			pkm_load5.Enabled = true;
			pkm_load6.Enabled = true;
			
			link_group.Enabled = true;
			
			
		}
		void iv_type(int iv, ComboBox box)
		{

				if (iv == 0xFF)//Random
					box.SelectedIndex = 0;
				else if (iv == 0xFE)//Priority
					box.SelectedIndex = 1;
				else if (iv > 31)//Unknown
					box.SelectedIndex = 3;
				else //Set
					box.SelectedIndex = 2;
		}
		void load_pkm(int index)
		{
			ot_name.Text = pokemonlink.Pokes[index].OT;
			ot_gender.SelectedIndex = pokemonlink.Pokes[index].Gender;
			tid.Value = pokemonlink.Pokes[index].TID;
			sid.Value = pokemonlink.Pokes[index].SID;
			
			nickname.Text = pokemonlink.Pokes[index].Nickname;
			species.SelectedIndex = pokemonlink.Pokes[index].Species;
			if(pokemonlink.Pokes[index].Nature == 0xFF)
				nature.SelectedIndex = 25;
			else
				nature.SelectedIndex = pokemonlink.Pokes[index].Nature;
			level.Value = pokemonlink.Pokes[index].Level;
			//form.SelectedIndex = pokemonlink.Pokes[index].Form;
			formindex.Value = pokemonlink.Pokes[index].Form;
			ability.SelectedIndex = pokemonlink.Pokes[index].AbilityType;
			helditem.SelectedIndex = pokemonlink.Pokes[index].HeldItem;
			gender.SelectedIndex = pokemonlink.Pokes[index].Gender;
			
			isegg.Checked = pokemonlink.Pokes[index].IsEgg;
			
			ball.SelectedIndex = pokemonlink.Pokes[index].Pokéball;
			
			language.SelectedIndex = pokemonlink.Pokes[index].Language;
			origin.SelectedIndex = pokemonlink.Pokes[index].OriginGame;
			
			encryption.Text = pokemonlink.Pokes[index].EncryptionConstant.ToString("X8");
			if (pokemonlink.Pokes[index].EncryptionConstant == 0)
				ec_type.SelectedIndex = 0;
			else
				ec_type.SelectedIndex = 1;
			pid.Text = pokemonlink.Pokes[index].PID.ToString("X8");
			pid_type.SelectedIndex =  pokemonlink.Pokes[index].PIDType;
			
			//IVs
			if(pokemonlink.Pokes[index].IV_HP <= 31){ iv_hp.Enabled = true; iv_hp.Value = pokemonlink.Pokes[index].IV_HP; }
			else {iv_hp.Enabled = false;}
			
			if(pokemonlink.Pokes[index].IV_ATK <= 31){ iv_atk.Enabled = true; iv_atk.Value = pokemonlink.Pokes[index].IV_ATK; }
			else {iv_atk.Enabled = false;}
			
			if(pokemonlink.Pokes[index].IV_DEF <= 31){ iv_def.Enabled = true; iv_def.Value = pokemonlink.Pokes[index].IV_DEF; }
			else {iv_def.Enabled = false;}
			
			if(pokemonlink.Pokes[index].IV_SPA <= 31){ iv_spa.Enabled = true; iv_spa.Value = pokemonlink.Pokes[index].IV_SPA; }
			else {iv_spa.Enabled = false;}
			
			if(pokemonlink.Pokes[index].IV_SPD <= 31){ iv_spd.Enabled = true; iv_spd.Value = pokemonlink.Pokes[index].IV_SPD; }
			else {iv_spd.Enabled = false;}
			
			if(pokemonlink.Pokes[index].IV_SPE <= 31){ iv_spe.Enabled = true; iv_spe.Value = pokemonlink.Pokes[index].IV_SPE; }
			else {iv_spe.Enabled = false;}

			iv_type(pokemonlink.Pokes[index].IV_HP, ivhp_type);
			iv_type(pokemonlink.Pokes[index].IV_ATK, ivatk_type);
			iv_type(pokemonlink.Pokes[index].IV_DEF, ivdef_type);
			iv_type(pokemonlink.Pokes[index].IV_SPA, ivspa_type);
			iv_type(pokemonlink.Pokes[index].IV_SPD, ivspd_type);
			iv_type(pokemonlink.Pokes[index].IV_SPE, ivspe_type);
			
			
			//No EVs?
			
			//Contest
			cnt_0.Value = pokemonlink.Pokes[index].CNT_Cool;
			cnt_1.Value = pokemonlink.Pokes[index].CNT_Beauty;
			cnt_2.Value = pokemonlink.Pokes[index].CNT_Cute;
			cnt_3.Value = pokemonlink.Pokes[index].CNT_Smart;
			cnt_4.Value = pokemonlink.Pokes[index].CNT_Tough;
			cnt_5.Value = pokemonlink.Pokes[index].CNT_Sheen;
			
			//Met data
			met_location.SelectedIndex = location2index(pokemonlink.Pokes[index].MetLocation);
			eggmetlocation.SelectedIndex = location2index(pokemonlink.Pokes[index].EggLocation);
			//No met date?
			met_level.Value = pokemonlink.Pokes[index].MetLevel;
			
			//Ribbons
			rib0_0.Checked = pokemonlink.Pokes[index].RIB0_0;
			rib0_1.Checked = pokemonlink.Pokes[index].RIB0_1;
			rib0_2.Checked = pokemonlink.Pokes[index].RIB0_2;
			rib0_3.Checked = pokemonlink.Pokes[index].RIB0_3;
			rib0_4.Checked = pokemonlink.Pokes[index].RIB0_4;
			rib0_5.Checked = pokemonlink.Pokes[index].RIB0_5;
			rib0_6.Checked = pokemonlink.Pokes[index].RIB0_6;
			rib0_7.Checked = pokemonlink.Pokes[index].RIB0_7;
			
			rib1_0.Checked = pokemonlink.Pokes[index].RIB1_0;
			rib1_1.Checked = pokemonlink.Pokes[index].RIB1_1;
			rib1_2.Checked = pokemonlink.Pokes[index].RIB1_2;
			rib1_3.Checked = pokemonlink.Pokes[index].RIB1_3;
			rib1_4.Checked = pokemonlink.Pokes[index].RIB1_4;
			rib1_5.Checked = pokemonlink.Pokes[index].RIB1_5;
			rib1_6.Checked = pokemonlink.Pokes[index].RIB1_6;
			
			//Moves
			move1.SelectedIndex = pokemonlink.Pokes[index].Move1;
			move2.SelectedIndex = pokemonlink.Pokes[index].Move2;
			move3.SelectedIndex = pokemonlink.Pokes[index].Move3;
			move4.SelectedIndex = pokemonlink.Pokes[index].Move4;
			
			relearn_1.SelectedIndex = pokemonlink.Pokes[index].RelearnMove1;
			relearn_2.SelectedIndex = pokemonlink.Pokes[index].RelearnMove2;
			relearn_3.SelectedIndex = pokemonlink.Pokes[index].RelearnMove3;
			relearn_4.SelectedIndex = pokemonlink.Pokes[index].RelearnMove4;
			
			
			//Memories
			if (pokemonlink.Pokes[index].OT_Feeling > 6)	memory_feeling.SelectedIndex = 7;
			else	memory_feeling.SelectedIndex = pokemonlink.Pokes[index].OT_Feeling;
			
			if (pokemonlink.Pokes[index].OT_Intensity > 23)	memory_intensity.SelectedIndex = 24;
			else	memory_intensity.SelectedIndex = pokemonlink.Pokes[index].OT_Intensity;
						
			if (pokemonlink.Pokes[index].OT_Memory > 72) memory_type.SelectedIndex = 73;
			else	memory_type.SelectedIndex = pokemonlink.Pokes[index].OT_Memory;
			//memory_textvar.SelectedIndex = pokemonlink.Pokes[index].OT_TextVar;
			
			switch (index)
			{
				case 0:
					transfer_flags.Text = "0x" + pokemonlink.PKM1_flags.ToString("X8");
					break;
				case 1:
					transfer_flags.Text = "0x" + pokemonlink.PKM2_flags.ToString("X8");
					break;;
				case 2:
					transfer_flags.Text = "0x" + pokemonlink.PKM3_flags.ToString("X8");
					break;
				case 3:
					transfer_flags.Text = "0x" + pokemonlink.PKM4_flags.ToString("X8");
					break;
				case 4:
					transfer_flags.Text = "0x" + pokemonlink.PKM5_flags.ToString("X8");
					break;
				case 5:
					transfer_flags.Text = "0x" + pokemonlink.PKM6_flags.ToString("X8");
					break;
			}

			
			pkm_groupBox.Enabled = true;
		}
		void Pkm_load1Click(object sender, EventArgs e)
		{
			load_pkm(0);
			slot1.BackColor = Color.LightGreen;
			slot2.BackColor = Color.LightGray;
			slot3.BackColor = Color.LightGray;
			slot4.BackColor = Color.LightGray;
			slot5.BackColor = Color.LightGray;
			slot6.BackColor = Color.LightGray;
		}
		void Pkm_load2Click(object sender, EventArgs e)
		{
			load_pkm(1);
			slot1.BackColor = Color.LightGray;
			slot2.BackColor = Color.LightGreen;
			slot3.BackColor = Color.LightGray;
			slot4.BackColor = Color.LightGray;
			slot5.BackColor = Color.LightGray;
			slot6.BackColor = Color.LightGray;
		}
		void Pkm_load3Click(object sender, EventArgs e)
		{
			load_pkm(2);
			slot1.BackColor = Color.LightGray;
			slot2.BackColor = Color.LightGray;
			slot3.BackColor = Color.LightGreen;
			slot4.BackColor = Color.LightGray;
			slot5.BackColor = Color.LightGray;
			slot6.BackColor = Color.LightGray;
		}
		void Pkm_load4Click(object sender, EventArgs e)
		{
			load_pkm(3);
			slot1.BackColor = Color.LightGray;
			slot2.BackColor = Color.LightGray;
			slot3.BackColor = Color.LightGray;
			slot4.BackColor = Color.LightGreen;
			slot5.BackColor = Color.LightGray;
			slot6.BackColor = Color.LightGray;
		}
		void Pkm_load5Click(object sender, EventArgs e)
		{
			load_pkm(4);
			slot1.BackColor = Color.LightGray;
			slot2.BackColor = Color.LightGray;
			slot3.BackColor = Color.LightGray;
			slot4.BackColor = Color.LightGray;
			slot5.BackColor = Color.LightGreen;
			slot6.BackColor = Color.LightGray;
		}
		void Pkm_load6Click(object sender, EventArgs e)
		{
			load_pkm(5);
			slot1.BackColor = Color.LightGray;
			slot2.BackColor = Color.LightGray;
			slot3.BackColor = Color.LightGray;
			slot4.BackColor = Color.LightGray;
			slot5.BackColor = Color.LightGray;
			slot6.BackColor = Color.LightGreen;
		}
		
		int location2index(int location)
		{
			int i = 0;
			for (i=0; i<locations.Length; i++)
			{
				if (locations[i] == location)
					return i;
			}
			return 0;
		}
		
		int[] locations = new int[]
		{
			0x0000, 0x0002, 0x0004, 0x0006, 0x0008, 0x0009, 0x000A, 0x000C, 0x000D, 0x000E,
			0x0010, 0x0011, 0x0012, 0x0014, 0x0015, 0x0016, 0x0018, 0x001A, 0x001C, 0x001D,
			0x001E, 0x0020, 0x0022, 0x0023, 0x0024, 0x0026, 0x0027, 0x0028, 0x002A, 0x002B,
			0x002C, 0x002E, 0x002F, 0x0030, 0x0032, 0x0033, 0x0034, 0x0036, 0x0037, 0x0038,
			0x003A, 0x003C, 0x003E, 0x003F, 0x0040, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046,
			0x0048, 0x004A, 0x004B, 0x004C, 0x004E, 0x004F, 0x0052, 0x0054, 0x0055, 0x0056,
			0x0058, 0x0059, 0x005A, 0x005C, 0x005D, 0x005E, 0x0060, 0x0061, 0x0062, 0x0064,
			0x0065, 0x0066, 0x0067, 0x0068, 0x006A, 0x006C, 0x006E, 0x0070, 0x0072, 0x0074,
			0x0076, 0x0078, 0x007A, 0x007C, 0x007E, 0x0080, 0x0082, 0x0084, 0x0086, 0x0087,
			0x0088, 0x008A, 0x008C, 0x008E, 0x0090, 0x0092, 0x0094, 0x0096, 0x0098, 0x009A,
			0x009C, 0x009E, 0x00A0, 0x00A2, 0x00A4, 0x00A6, 0x00A8, 0x00AA, 0x00AC, 0x00AE,
			0x00B0, 0x00B2, 0x00B4, 0x00B6, 0x00B8, 0x00BA, 0x00BC, 0x00BE, 0x00C0, 0x00C2,
			0x00C4, 0x00C6, 0x00C8, 0x00CA, 0x00CC, 0x00CE, 0x00D0, 0x00D2, 0x00D4, 0x00D6,
			0x00D8, 0x00DA, 0x00DC, 0x00DE, 0x00E0, 0x00E2, 0x00E4, 0x00E6, 0x00E8, 0x00EA,
			0x00EC, 0x00EE, 0x00F0, 0x00F2, 0x00F4, 0x00F6, 0x00F8, 0x00FA, 0x00FC, 0x00FE,
			0x0100, 0x0102, 0x0104, 0x0106, 0x0108, 0x010A, 0x010C, 0x010E, 0x0110, 0x0112,
			0x0114, 0x0116, 0x0118, 0x011A, 0x011C, 0x011E, 0x0120, 0x0122, 0x0124, 0x0126,
			0x0128, 0x012A, 0x012C, 0x012E, 0x0130, 0x0132, 0x0134, 0x0136, 0x0138, 0x013A,
			0x013C, 0x013E, 0x0140, 0x0142, 0x0144, 0x0146, 0x0148, 0x014A, 0x014C, 0x014E,
			0x0150, 0x0152, 0x0154, 0x0156, 0x0158, 0x015A, 0x015C, 0x015E, 0x0160, 0x0162,
			0x7531, 0x7532, 0x7533, 0x7534, 0x7535, 0x7536, 0x7537, 0x7538, 0x7539, 0x753A,
			0x753B, 0x9C41, 0x9C42, 0x9C43, 0x9C44, 0x9C45, 0x9C46, 0x9C47, 0x9C48, 0x9C49,
			0x9C4A, 0x9C4B, 0x9C4C, 0x9C4D, 0x9C4E, 0x9C4F, 0x9C50, 0x9C51, 0x9C52, 0x9C53,
			0x9C54, 0x9C55, 0x9C56, 0x9C57, 0x9C58, 0x9C59, 0x9C5A, 0x9C5B, 0x9C5C, 0x9C5D,
			0x9C5E, 0x9C5F, 0x9C60, 0x9C61, 0x9C62, 0x9C63, 0x9C64, 0x9C65, 0x9C66, 0x9C67,
			0x9C68, 0x9C69, 0x9C6A, 0x9C6B, 0x9C6C, 0x9C6D, 0x9C6E, 0x9C6F, 0x9C70, 0x9C71,
			0x9C72, 0x9C73, 0x9C74, 0x9C75, 0x9C76, 0x9C77, 0x9C78, 0x9C79, 0x9C7A, 0x9C7B,
			0x9C7C, 0x9C7D, 0x9C7E, 0x9C7F, 0x9C80, 0x9C81, 0x9C82, 0x9C83, 0x9C84, 0x9C85,
			0x9C86, 0x9C87, 0x9C88, 0x9C89, 0x9C8A, 0x9C8B, 0x9C8C, 0x9C8D, 0x9C8E, 0x9C8F,
			0xEA61, 0xEA62, 0xEA63, 0xEA64
		};
		void Load_pl6Click(object sender, EventArgs e)
		{
            OpenFileDialog openFD = new OpenFileDialog();
            //openFD.InitialDirectory = "c:\\";
            openFD.Filter = "Pokémon Link Data|*.bin|All Files (*.*)|*.*";
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                #region filename
                linkfile = openFD.FileName;
                #endregion
                System.IO.FileStream saveFile;
	            saveFile = new FileStream(linkfile, FileMode.Open);
	            if (saveFile.Length != 0xA47){
	            	//linkfile = "";
	            	MessageBox.Show("Invalid file length", "Error");
	            	return;
	            }
	            pl6_path.Text = linkfile;
	            byte[] temp = new byte[0xA47];
	            ReadWholeArray(saveFile, temp);
	            pokemonlink = new PL6(temp);
				
	            //Load pkmlink to editor
                linkedit_load();
            }

		}

	}
}
