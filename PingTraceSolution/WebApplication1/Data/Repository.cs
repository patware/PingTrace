using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace WebApplication1.Data
{
    public class Repository : IRepository
    {
        public string RunPing()
        {
            var connext = new System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings["Database1"].ConnectionString);

            var cmd = new System.Data.SqlClient.SqlCommand("[PingTrace].[spPing]", connext);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            connext.Open();
            var res = cmd.ExecuteReader();

            if (res.Read())
            {
                return res.GetString(0);
            }
            else
            {
                return string.Empty;
            }

        }

        public IList<Patware.PingTrace.Core.TraceDestination> RunTraces()
        {
            var connext = new System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings["Database1"].ConnectionString);

            var cmd = new System.Data.SqlClient.SqlCommand("[PingTrace].[spTraces]", connext);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            connext.Open();
            var res = cmd.ExecuteReader();

            var l = new List<Patware.PingTrace.Core.TraceDestination>();

            while (res.Read())
            {
                /*
                 * 0                                    1           2                   3                   4                   5                       6                   7           
                 * Id	                                Name	    ExpectedIdentity	ExpectedMachineName	ElapsedMaxSeconds	ElapsedAverageSeconds	PayloadDescription	PayloadRegex
                 * 188712E4-4E7E-417E-8C91-D9115BC0BE22	Database1	NULL	            SIAD049W\SQLEXPRESS	1	                1	                    Pong	            Pong
                 * 
                 */
                var td = new Patware.PingTrace.Core.TraceDestination();
                td.Id = res.GetGuid(0);
                td.Name = res.GetString(1);
                td.ExpectedIdentity = res.IsDBNull(2) ? string.Empty : res.GetString(2);
                td.ExpectedMachineName = res.GetString(3);
                td.ElapsedMaxSeconds = res.GetInt32(4);
                td.ElapsedAverageSeconds = res.GetInt32(5);
                td.PayloadDescription = res.GetString(6);
                td.PayloadRegex = res.GetString(7);

                l.Add(td);
            }

            return l;
        }

        public Patware.PingTrace.Core.TraceResult RunTrace(string destination)
        {
            
            var connext = new System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings["Database1"].ConnectionString);

            var cmd = new System.Data.SqlClient.SqlCommand("[PingTrace].[spTrace]", connext);
            cmd.Parameters.AddWithValue("@destination", destination);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            connext.Open();
            var res = cmd.ExecuteReader();

            if (res.Read())
            {
                var ret = new Patware.PingTrace.Core.TraceResult();
                
                ret.Id = res.GetGuid(0);
                ret.Name = res.GetString(1);
                ret.Identity = res.GetString(2);
                ret.MachineName = res.GetString(3);
                ret.StartedAt = res.GetDateTime(4);
                ret.FinishedAt = res.GetDateTime(5);
                ret.Payload = res.GetString(6);

                return ret;
            }
            
            return null;
            

        }
    }
}