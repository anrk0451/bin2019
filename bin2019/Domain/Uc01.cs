﻿using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bin2019.Domain
{
	class Uc01
	{
		[SugarColumn(IsPrimaryKey = true)]
		public string uc001 { get; set; }   //操作员编号

		public string uc002 { get; set; }   //操作员代码
		public string uc003 { get; set; }   //操作员姓名
		public string uc004 { get; set; }   //密码
		public string status { get; set; }  //状态 1-正常 0-删除 2-冻结 
	}
}
