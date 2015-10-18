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
		private void PDR_read_data()
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
		            saveFile.Close();
	            }else if (saveFile.Length == 0x76000){
	            	game = 2;
	            	currgame.Text = "OR/AS";
		            ReadWholeArray(saveFile, savebuffer_ORAS);
		            saveFile.Close();
	            }
		}
		private void PDR_get_data()
        {
            OpenFileDialog openFD = new OpenFileDialog();
            //openFD.InitialDirectory = "c:\\";
            openFD.Filter = "VI gen save data|main|All Files (*.*)|*.*";
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                #region filename
                savegamename.Text = openFD.FileName;
                #endregion
                PDR_read_data();
            }
            
        }
		private void PDR_save_data()
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
		private void PDR_dump_forest_data()
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
		private void PDR_read_forest_data()
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
	            PDR_injectNsave();
		}
		private void PDR_get_forest_data()
        {
            OpenFileDialog openFD = new OpenFileDialog();
            //openFD.InitialDirectory = "c:\\";
            openFD.Filter = "Pokémon Link Data|*.bin|All Files (*.*)|*.*";
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                #region filename
                linkfile = openFD.FileName;
                #endregion
                PDR_read_forest_data();
            }
            
        }
		private void PDR_injectNsave()
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
			PDR_save_data();
		}
		void Button1Click(object sender, EventArgs e)
		{
			PDR_get_data();
		}
		void Dump_butClick(object sender, EventArgs e)
		{
			if (savegamename.Text.Length < 1) return;
			PDR_dump_forest_data();
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
			PDR_get_forest_data();
		}

	}
}
