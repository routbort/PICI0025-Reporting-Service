
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.IO;
using System.ComponentModel;


public class DataInterface
{
    public DataInterface(string connectionString)
    {
        this.ConnectionString = connectionString;
    }

    public string ConnectionString { get; private set; }

    public DataTable GetPendingReportDeliveries()
    {


        using (SqlConnection connection = new SqlConnection(this.ConnectionString))
        using (SqlCommand command = new SqlCommand
        {
            Connection = connection,
            CommandText = "select * from dbo.a_PICI0025_report_delivery_queue_mdacc where delivery_date is null and error_count<3",
            CommandType = CommandType.Text
        })
        {

            DataSet dataSet = new DataSet();
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            connection.Open();
            adapter.Fill(dataSet);
            connection.Close();
            return dataSet.Tables[0];

        }

    }

    public DataTable GetTrialData()
    {
        using (SqlConnection connection = new SqlConnection(this.ConnectionString))
        using (SqlCommand command = new SqlCommand
        {
            Connection = connection,
            CommandText = "stprc_clinical_trials_get_worksheet_data",
            CommandType = CommandType.StoredProcedure
        })
        {
            command.Parameters.AddWithValue("@worksheet_name", "PICI 0025 Trial");
            DataSet dataSet = new DataSet();
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            connection.Open();
            adapter.Fill(dataSet);
            connection.Close();
            return dataSet.Tables[0];
        }

    }

    public void MarkReportAsDelivered(int acc_report_id, string ErrorMessage)
    {

        using (SqlConnection connection = new SqlConnection(this.ConnectionString))
        using (SqlCommand command = new SqlCommand())
        {

            command.Connection = connection;
            command.CommandType = CommandType.Text;
            if (ErrorMessage == null)
                command.CommandText = "update dbo.a_PICI0025_report_delivery_queue_mdacc set is_error=0,delivery_date = GETDATE() where acc_report_id = " + acc_report_id.ToString();
            else
            {
                command.CommandText = "update dbo.a_PICI0025_report_delivery_queue_mdacc set is_error=1,error_message = @error_message, error_count = error_count+1 where acc_report_id = " + acc_report_id.ToString();
                command.Parameters.AddWithValue("@error_message", ErrorMessage);
            }

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }
    }

}


