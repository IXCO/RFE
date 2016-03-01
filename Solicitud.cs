using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFE
{
    class Solicitud
    {
        public int society;
        public String department;
        public String sender;
        public String total;
        public String invoiceNo;
        public String uuid;
        public String requesterEmail;
        public String chiefEmail;
        public Solicitud(int societyId,String departmentId, String senderName, String totalCost)
        {
            society = societyId;
            department = departmentId;
            sender = senderName;
            total = totalCost;
            invoiceNo = "";
        }
        public Solicitud(int societyId, String departmentId, String senderName, String totalCost, String noInvoice)
        {

            society = societyId;
            department = departmentId;
            sender = senderName;
            total = totalCost;
            invoiceNo = noInvoice;
        }
        

    }
}
