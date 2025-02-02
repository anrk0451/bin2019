﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Bin2019.BaseObject;
using Oracle.ManagedDataAccess.Client;
using Bin2019.Misc;
using Bin2019.Action;
using Bin2019.Domain;

namespace Bin2019.windows
{
	public partial class Frm_registerOut : MyDialog
	{
		private string rc001 = string.Empty;
		private decimal price = decimal.Zero;  //寄存单价
		private bool isrefund = false;         //是否退费

		public Frm_registerOut()
		{
			InitializeComponent();
		}

		private void Frm_registerOut_Load(object sender, EventArgs e)
		{
			rc001 = this.swapdata["RC001"].ToString();

			OracleDataReader reader = SqlAssist.ExecuteReader("select * from rc01 where rc001='" + rc001 + "'");
			while (reader.Read())
			{
				txtEdit_rc001.Text = rc001;
				txtEdit_rc109.EditValue = reader["RC109"];
				txtEdit_rc003.EditValue = reader["RC003"];
				txtEdit_rc303.EditValue = reader["RC303"];
				rg_rc002.EditValue = reader["RC002"];
				rg_rc202.EditValue = reader["RC202"];
				txtEdit_rc004.EditValue = reader["RC004"];
				txtEdit_rc404.EditValue = reader["RC404"];
				txtEdit_rc150.EditValue = reader["RC150"];   //寄存到期日期
				be_position.EditValue = RegisterAction.GetRegPathName(rc001);

				price = RegisterAction.GetBitPrice(reader["RC130"].ToString());
				txtEdit_price.EditValue = price;

				int diff = RegisterAction.CalcOutDiffDays(rc001);

				int compare = string.Compare(Convert.ToDateTime(reader["RC150"]).ToString("yyyyMMdd"), DateTime.Now.ToString("yyyyMMdd"));
				if (compare == 0)
				{
					checkEdit1.Enabled = false;
					txtEdit_nums.Enabled = false;
				}
				else if (compare > 0)  //退费
				{
					lc_1.Text = "剩余天数";
					lc_2.Text = "应退费年份(年限)";
					lc_3.Text = "退费金额";
					isrefund = true;
					///宾县不用退费
					txtEdit_nums.EditValue = 0;
					txtEdit_fee.EditValue = 0;
				}
				else
				{
					lc_1.Text = "过期天数";
					lc_2.Text = "应补费年份(年限)";
					lc_3.Text = "补费金额";

					txtEdit_nums.EditValue = Math.Round((diff * 1.0f) / 365, 2);
					txtEdit_fee.EditValue = Convert.ToDecimal(Math.Round((diff * 1.0f) / 365, 2)) * price;
				}

				
				txtEdit_diff.EditValue = diff;
				
			}

			////是否允许取消迁出补退 /////
			if (MiscAction.GetRight(Envior.cur_userId, "02070") == "0")
			{
				checkEdit1.Enabled = false;
			}
		}

		private void CheckEdit1_CheckedChanged(object sender, EventArgs e)
		{
			txtEdit_nums.Enabled = checkEdit1.Checked;
			if (!checkEdit1.Checked)
			{
				txtEdit_nums.Text = "0.00";
			}
		}

		/// <summary>
		/// 补退费年限
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TxtEdit_nums_Validating(object sender, CancelEventArgs e)
		{
			if (!string.IsNullOrEmpty(txtEdit_nums.Text))
			{
				if (Convert.ToDecimal(txtEdit_nums.Text) < 0)
				{
					txtEdit_nums.ErrorImageOptions.Alignment = ErrorIconAlignment.MiddleRight;
					txtEdit_nums.ErrorText = "应为正值!";
					e.Cancel = true;
				}
			}
		}

		/// <summary>
		/// 补退费 年限变更
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TxtEdit_nums_EditValueChanged(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(txtEdit_nums.Text))
			{
				decimal nums = Convert.ToDecimal(txtEdit_nums.Text);
				txtEdit_fee.EditValue = Math.Round(price * nums);
			}
		}

		private void B_ok_Click(object sender, EventArgs e)
		{
			if (rc001 == null)
			{
				MessageBox.Show("数据传递错误!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if (txtEdit_oc003.EditValue == null || txtEdit_oc003.EditValue is System.DBNull)
			{
				txtEdit_oc003.ErrorImageOptions.Alignment = ErrorIconAlignment.MiddleRight;
				txtEdit_oc003.ErrorText = "请输入迁出办理人!";
				return;
			}
			if (mem_oc005.EditValue == null)
			{
				mem_oc005.ErrorImageOptions.Alignment = ErrorIconAlignment.MiddleRight;
				mem_oc005.ErrorText = "请输入迁出原因!";
				return;
			}
			string s_oc003 = txtEdit_oc003.Text;   //迁出人
			string s_oc005 = mem_oc005.Text;       //迁出原因
			string s_oc004 = txtEdit_oc004.Text;   //迁出人身份证号

			int diff = int.Parse(txtEdit_diff.EditValue.ToString());
			decimal nums = decimal.Zero;
			string fa001 = Tools.GetEntityPK("FA01");

			//补退情况
			if (checkEdit1.Checked)
			{
				nums = decimal.Parse(txtEdit_nums.Text);
			}
			else
			{
				nums = 0;
			}

			if (MessageBox.Show("确认要继续办理迁出吗？本业务将不能回退!", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.No) return;
			if( Convert.ToDecimal(txtEdit_fee.Text) > 0 && Envior.cur_userId != AppInfo.ROOTID)
			{
				MessageBox.Show("当前记录已经欠费,不能迁出!","提示",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
				return;
			}
			int re = RegisterAction.RegisterOut(rc001,
												 s_oc003,
												 s_oc004,
												 s_oc005,
												 diff,
												 fa001,
												 price,
												 isrefund ? 0 - nums : nums,
												 Envior.cur_userId
				);
			if (re > 0)
			{
				MessageBox.Show("迁出办理成功!现在打印【迁出通知单】", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
				PrtServAction.PrtRegisterOutNotice(rc001 );				

				if (Math.Abs(nums) > 0)
				{
					if (MessageBox.Show("缴费成功!现在打印【发票】吗?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
					{
						OracleParameter op_rc001 = new OracleParameter("ic_rc001", OracleDbType.Varchar2, 10);
						op_rc001.Direction = ParameterDirection.Input;
						op_rc001.Value = rc001;

						string cuid = SqlAssist.ExecuteScalar("select rc014 from rc01 where rc001 = :rc001", new OracleParameter[] { op_rc001 }).ToString();
						string cuname = txtEdit_rc003.Text;

						//输入交款人信息
						Frm_InvoiceClientName frm_clientName = new Frm_InvoiceClientName(cuname,cuid);
						if (frm_clientName.ShowDialog() == DialogResult.OK)
						{
							cuname = frm_clientName.swapdata["cuname"].ToString();
							cuid = frm_clientName.swapdata["cuid"].ToString();
							frm_clientName.Dispose();

							if(InvoiceAction.UpdateCustomerInfo(fa001,cuid,cuname) < 0)
							{
								return;
							}
						}
						else
						{
							frm_clientName.Dispose();
							return;
						}
 
						if (InvoiceAction.GetInvoiceNextNum(Envior.invoice_pBillBatchCode) > 0)
						{
							string s_tip = "下一张发票号:" + Envior.NEXT_BILL_NUM + "\r\n" +
										   "发票代码:" + Envior.NEXT_BILL_CODE;
							if (MessageBox.Show(s_tip, "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
							{
								InvoiceAction.Invoice(fa001);
							}
						}
					}
				}
			}
			DialogResult = DialogResult.OK;
			this.Close();

		}
	}
}