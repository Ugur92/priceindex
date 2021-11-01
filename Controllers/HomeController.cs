using Dapper;
using DataAccess;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using LedSiparisModulu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LedSiparisModulu.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private const string GrupBazindaOrtalamaSatis = "SELECT * FROM (SELECT CASE WHEN I.SPECODE='102' THEN '102-MİGROS ÜRÜNLERİ' WHEN I.SPECODE= '100' THEN '100-RENDERİNG ÜRÜNLERİ' WHEN I.SPECODE= '103' THEN '103-A101 ÜRÜNLERI' WHEN I.SPECODE= '104' THEN '104-RENDERİNG İŞLENMEMİŞ' WHEN I.SPECODE= '10' THEN '10-ZİYAFET SERİSİ' WHEN I.SPECODE= '11' THEN '11-BOYUNLU BÜTÜN PİLİÇ' WHEN I.SPECODE= '12' THEN '12-BOYUNSUZ BÜTÜN PİLİÇ' WHEN I.SPECODE= '13' THEN '13-ÇEVİRMELİK PİLİÇ' WHEN I.SPECODE= '21' THEN '21-GÖĞÜS ÜRÜNLERİ' WHEN I.SPECODE= '31' THEN '31-BUT ÜRÜNLERİ' WHEN I.SPECODE= '41' THEN '41-KANAT ÜRÜNLERİ' WHEN I.SPECODE= '51' THEN '51-SAKATAT ÜRÜNLERİ' WHEN I.SPECODE= '61' THEN '61-ÖZEL ÜRÜNLER' WHEN I.SPECODE= '71' THEN '71-İLERİ İŞLEM ÜRÜNLERİ' WHEN I.SPECODE= '81' THEN '81-PENÇE' WHEN I.SPECODE= '82' THEN '82-KANAT UCU' WHEN I.SPECODE= '91' THEN '91-YAN ÜRÜNLER' WHEN I.SPECODE= '92' THEN '92-MDM ÜRÜNLERİ' END AS URUNGRUBU,ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) 'SatisMiktari', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) 'IadeMiktari', (ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0))/1000 'NetSatisMiktari', ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) 'SatisTutari', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) 'IadeTutari', CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END AS 'IndirimTutari',ISNULL(SUM(SIGN(L.IOCODE - 1) * ISNULL(L.TOTAL, 0)), 0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END) 'NetSatisTutari' , NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END),0),0)  / NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0),0) ,0) 'NetBirimFiyati' , I.SPECODE FROM LG_051_01_STLINE L LEFT OUTER JOIN LG_051_01_STFICHE F ON L.STFICHEREF=F.LOGICALREF LEFT OUTER JOIN LG_051_CLCARD ch ON F.CLIENTREF = ch.LOGICALREF LEFT OUTER JOIN LG_051_ITEMS I ON L.STOCKREF= I.LOGICALREF AND L.LINETYPE NOT IN (1,2,3,4) LEFT OUTER JOIN LG_051_UNITSETF U ON I.UNITSETREF=U.LOGICALREF LEFT OUTER JOIN LG_051_UNITSETL T ON U.LOGICALREF=T.UNITSETREF WHERE (F.DATE_>=@T1 AND F.DATE_<= @T2) AND I.LOGICALREF IS NOT NULL AND L.CANCELLED NOT IN (1) AND F.GRPCODE=2 AND T.LINENR= 1 AND I.CARDTYPE=12";

        private const string GrupBazindaOrtalamaSatisOncesi = "SELECT * FROM (SELECT CASE WHEN I.SPECODE='102' THEN '102-MİGROS ÜRÜNLERİ' WHEN I.SPECODE= '100' THEN '100-RENDERİNG ÜRÜNLERİ' WHEN I.SPECODE= '103' THEN '103-A101 ÜRÜNLERI' WHEN I.SPECODE= '104' THEN '104-RENDERİNG İŞLENMEMİŞ' WHEN I.SPECODE= '10' THEN '10-ZİYAFET SERİSİ' WHEN I.SPECODE= '11' THEN '11-BOYUNLU BÜTÜN PİLİÇ' WHEN I.SPECODE= '12' THEN '12-BOYUNSUZ BÜTÜN PİLİÇ' WHEN I.SPECODE= '13' THEN '13-ÇEVİRMELİK PİLİÇ' WHEN I.SPECODE= '21' THEN '21-GÖĞÜS ÜRÜNLERİ' WHEN I.SPECODE= '31' THEN '31-BUT ÜRÜNLERİ' WHEN I.SPECODE= '41' THEN '41-KANAT ÜRÜNLERİ' WHEN I.SPECODE= '51' THEN '51-SAKATAT ÜRÜNLERİ' WHEN I.SPECODE= '61' THEN '61-ÖZEL ÜRÜNLER' WHEN I.SPECODE= '71' THEN '71-İLERİ İŞLEM ÜRÜNLERİ' WHEN I.SPECODE= '81' THEN '81-PENÇE' WHEN I.SPECODE= '82' THEN '82-KANAT UCU' WHEN I.SPECODE= '91' THEN '91-YAN ÜRÜNLER' WHEN I.SPECODE= '92' THEN '92-MDM ÜRÜNLERİ' END AS URUNGRUBU,ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) 'SatisMiktari', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) 'IadeMiktari', (ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0))/1000 'NetSatisMiktari', ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) 'SatisTutari', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) 'IadeTutari', CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END AS 'IndirimTutari',ISNULL(SUM(SIGN(L.IOCODE - 1) * ISNULL(L.TOTAL, 0)), 0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END) 'NetSatisTutari' , NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END),0),0)  / NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0),0) ,0) 'NetBirimFiyati' , I.SPECODE FROM LG_051_01_STLINE L LEFT OUTER JOIN LG_051_01_STFICHE F ON L.STFICHEREF=F.LOGICALREF LEFT OUTER JOIN LG_051_CLCARD ch ON F.CLIENTREF = ch.LOGICALREF LEFT OUTER JOIN LG_051_ITEMS I ON L.STOCKREF= I.LOGICALREF AND L.LINETYPE NOT IN (1,2,3,4) LEFT OUTER JOIN LG_051_UNITSETF U ON I.UNITSETREF=U.LOGICALREF LEFT OUTER JOIN LG_051_UNITSETL T ON U.LOGICALREF=T.UNITSETREF WHERE (F.DATE_>=@T2 - 1 AND F.DATE_<= @T2-1) AND I.LOGICALREF IS NOT NULL AND L.CANCELLED NOT IN (1) AND F.GRPCODE=2 AND T.LINENR= 1 AND I.CARDTYPE= 12";
       
        private const string ButunParcaOrtalamaSatisFiyatlari = "SELECT * FROM (SELECT I.CATEGORYNAME AS 'UrunGrubu', ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) 'SatisMiktari', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) 'IadeMiktari', (ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0))/1000 'NetSatisMiktari', ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) 'SatisTutari', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) 'IadeTutari',  CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0  ELSE ISNULL(SUM(L.DISTCOST),0) END AS 'IndirimTutari',ISNULL(SUM(SIGN(L.IOCODE - 1) * ISNULL(L.TOTAL, 0)), 0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END) 'NetSatisTutari' , NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END),0),0) / NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0),0) ,0) 'NetBirimFiyati' FROM LG_051_01_STLINE L LEFT OUTER JOIN LG_051_01_STFICHE F ON L.STFICHEREF=F.LOGICALREF LEFT OUTER JOIN LG_051_CLCARD ch ON F.CLIENTREF = ch.LOGICALREF LEFT OUTER JOIN LG_051_ITEMS I ON L.STOCKREF= I.LOGICALREF AND L.LINETYPE NOT IN (1,2,3,4) LEFT OUTER JOIN LG_051_UNITSETF U ON I.UNITSETREF=U.LOGICALREF LEFT OUTER JOIN LG_051_UNITSETL T ON U.LOGICALREF=T.UNITSETREF WHERE (F.DATE_>=@T1 AND F.DATE_<=@T2) AND I.LOGICALREF IS NOT NULL AND L.CANCELLED NOT IN (1) AND F.GRPCODE=2 AND T.LINENR= 1 AND I.CARDTYPE= 12";
        
        private const string Graphic7liGrup = "CREATE TABLE #graphicveri( PARCAGRUBU NVARCHAR(250), NETMIKTAR numeric(18,1), NETSATISTUTARI numeric(18,2), NETFIYAT numeric(18,2), ORAN numeric(18,2)) INSERT INTO #graphicveri(PARCAGRUBU, NETMIKTAR, NETSATISTUTARI, NETFIYAT) SELECT[Parça Grubu],  (CONVERT(numeric(18,1), NETMIKTAR)) as NETMIKTAR, [NetSatisTutari], NETFIYAT FROM(SELECT I.KEYWORD1 AS 'Parça Grubu',ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) 'Satış Miktarı', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) 'İade Miktarı', ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) AS NETMIKTAR,ISNULL(SUM(SIGN(L.IOCODE - 1) * ISNULL(L.TOTAL, 0)), 0) 'Satış Tutarı',ISNULL(SUM(SIGN(4 - L.IOCODE) * ISNULL(L.TOTAL, 0)), 0) 'İade Tutarı',CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0)  THEN 0  ELSE ISNULL(SUM(L.DISTCOST),0) END AS 'İndirim Tutarı',ISNULL(SUM(SIGN(L.IOCODE - 1) * ISNULL(L.TOTAL, 0)), 0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END) 'NetSatisTutari' , NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0  ELSE ISNULL(SUM(L.DISTCOST),0) END),0),0) / NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0),0),0) NETFIYAT FROM LG_051_01_STLINE L LEFT OUTER JOIN LG_051_01_STFICHE F ON L.STFICHEREF=F.LOGICALREF LEFT OUTER JOIN LG_051_CLCARD ch ON F.CLIENTREF = ch.LOGICALREF LEFT OUTER JOIN LG_051_ITEMS I ON L.STOCKREF= I.LOGICALREF AND L.LINETYPE NOT IN (1,2,3,4) LEFT OUTER JOIN LG_051_UNITSETF U ON I.UNITSETREF=U.LOGICALREF LEFT OUTER JOIN LG_051_UNITSETL T ON U.LOGICALREF=T.UNITSETREF WHERE (F.DATE_>=@T1 AND F.DATE_<=@T2) AND I.LOGICALREF IS NOT NULL AND L.CANCELLED NOT IN (1) AND F.GRPCODE=2 AND T.LINENR= 1 AND I.CARDTYPE= 12";
      
        private const string GenelOrtalamaFiyat = "SELECT CONVERT(VARCHAR,DATE_,104) AS TARIH, CONVERT(numeric(18,2),NETFIYAT) AS NETFIYAT FROM ( SELECT L.DATE_, I.CARDTYPE 'Malzeme Türü',ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) 'Satış Miktarı', ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) 'İade Miktarı', ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) AS NETMIKTAR,ISNULL(SUM(SIGN(L.IOCODE - 1) * ISNULL(L.TOTAL, 0)), 0) 'Satış Tutarı',ISNULL(SUM(SIGN(4 - L.IOCODE) * ISNULL(L.TOTAL, 0)), 0) 'İade Tutarı',CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END AS 'İndirim Tutarı',ISNULL(SUM(SIGN(L.IOCODE - 1) * ISNULL(L.TOTAL, 0)), 0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END) 'NetSatisTutari' , NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.TOTAL,0)),0) - (ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.TOTAL,0)),0) + CASE WHEN ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) = ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0) THEN 0 ELSE ISNULL(SUM(L.DISTCOST),0) END),0),0) / NULLIF(ISNULL(ISNULL(SUM(SIGN(L.IOCODE-1)*ISNULL(L.AMOUNT,0)),0) - ISNULL(SUM(SIGN(4-L.IOCODE)*ISNULL(L.AMOUNT,0)),0),0),0) NETFIYAT FROM LG_051_01_STLINE L LEFT OUTER JOIN LG_051_01_STFICHE F ON L.STFICHEREF=F.LOGICALREF LEFT OUTER JOIN LG_051_CLCARD ch ON F.CLIENTREF = ch.LOGICALREF LEFT OUTER JOIN LG_051_ITEMS I ON L.STOCKREF= I.LOGICALREF AND L.LINETYPE NOT IN (1,2,3,4) LEFT OUTER JOIN LG_051_UNITSETF U ON I.UNITSETREF=U.LOGICALREF LEFT OUTER JOIN LG_051_UNITSETL T ON U.LOGICALREF=T.UNITSETREF WHERE (F.DATE_>=@T1 AND F.DATE_<=@T2) AND I.LOGICALREF IS NOT NULL AND L.CANCELLED NOT IN (1) AND F.GRPCODE=2 AND T.LINENR= 1 AND I.CARDTYPE= 12";

        
        private readonly LAF_BUPILICContext db;
        private readonly LogoDbContext logodb;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;

        public HomeController(LAF_BUPILICContext context,
            LogoDbContext logocontext,
            IConfiguration configuration,
            ILogger<HomeController> logger)
        {
            db = context;
            logodb = logocontext;
            _configuration = configuration;
            _logger = logger;
        }


        [HttpGet("")]
        [HttpGet("anasayfa", Name = "Anasayfa")]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetUrunGrupBazinda(IDataTablesRequest request)
        {
            DateTime? baslangicTarihi = null, bitisTarihi = null;
            string cariTip = null;
            bool ihracHaric = false, zincirHaric = false;

            if (request.AdditionalParameters != null)
            {
                if (request.AdditionalParameters.TryGetValue("baslangicTarihi", out object baslangicTarihiString))
                {
                    baslangicTarihi = DateTime.Parse(baslangicTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("bitisTarihi", out object bitisTarihiString))
                {
                    bitisTarihi = DateTime.Parse(bitisTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("cariTip", out object cariTipString))
                {
                    cariTip = cariTipString.ToString();
                }
                if (request.AdditionalParameters.TryGetValue("ihracHaric", out object ihracHaricDeger))
                {
                    ihracHaric = bool.Parse((string)ihracHaricDeger);
                }
                if (request.AdditionalParameters.TryGetValue("zincirHaric", out object zincirHaricDeger))
                {
                    zincirHaric = bool.Parse((string)zincirHaricDeger);
                }
            }

            long userID = long.Parse(HttpContext.User.FindFirst("Id").Value);
            string kod = HttpContext.User.FindFirst("Chkod").Value;
            var carikod = logodb.LG_051_CLCARD.AsNoTracking().FirstOrDefault(x => x.Code == kod);

            using (SqlConnection logoConnection = new SqlConnection(_configuration.GetSection("ConnectionString:LogoConnection").Value))
            {
                IEnumerable<GrupBazindaFiyat> sorgu = Enumerable.Empty<GrupBazindaFiyat>();
                StringBuilder query = new StringBuilder(GrupBazindaOrtalamaSatis);

                if (ihracHaric==true)
                {
                    query.Append("AND ch.CODE NOT LIKE '120.İHR%'");
                }
                if (zincirHaric==true)
                {
                    query.Append("AND CH.SPECODE4 IN ('12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009')");
                }

                string lokasyon = "'12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009'";
                if (cariTip == "Bayiler")
                {
                    lokasyon = "'12-001'";
                }
                else if (cariTip == "ZincirMarketler")
                {
                    lokasyon = "'12-002'";
                }
                else if (cariTip == "OrtakAlicilar")
                {
                    lokasyon = "'12-003'";
                }
                else if (cariTip == "MerkezDagitim")
                {
                    lokasyon = "'12-004'";
                }
                else if (cariTip == "IhracatMusterileri")
                {
                    lokasyon = "'12-005'";
                }
                else if (cariTip == "Subeler")
                {
                    lokasyon = "'12-006'";
                }
                else if (cariTip == "YerelZincir")
                {
                    lokasyon = "'12-007'";
                }
                else if (cariTip == "KantinYemekhane")
                {
                    lokasyon = "'12-009'";
                }

                query.Append("AND CH.SPECODE4 IN (" + lokasyon + ")");
                query.Append("AND I.SPECODE IN ('102','103','104','10','11','12','13','21','31','41','51','61','71','81','82','91','92') GROUP BY  I.SPECODE ) AS DYNMQRY WHERE [NetSatisTutari]>0 ORDER BY DYNMQRY.URUNGRUBU ASC");

                sorgu = logoConnection.Query<GrupBazindaFiyat>(query.ToString(), new { T1 = baslangicTarihi, T2 = bitisTarihi });
                if (request.Search.Value != null)
                {
                    sorgu = sorgu.Where(_item => _item.URUNGRUBU.Contains(request.Search.Value));
                }

                var dataPage = sorgu.OrderByDescending(o => o.URUNGRUBU);/*.Skip(request.Start).Take(request.Length).ToList()*/
                var response = DataTablesResponse.Create(request, sorgu.Count(), sorgu.Count(), dataPage);
                return new DataTablesJsonResult(response);
            }
        }

        public IActionResult GetUrunGrupBazindaOncesi(IDataTablesRequest request)
        {
            DateTime? baslangicTarihi = null, bitisTarihi = null;
            string cariTip = null;
            bool ihracHaric = false, zincirHaric = false;

            if (request.AdditionalParameters != null)
            {
                if (request.AdditionalParameters.TryGetValue("baslangicTarihi", out object baslangicTarihiString))
                {
                    baslangicTarihi = DateTime.Parse(baslangicTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("bitisTarihi", out object bitisTarihiString))
                {
                    bitisTarihi = DateTime.Parse(bitisTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("cariTip", out object cariTipString))
                {
                    cariTip = cariTipString.ToString();
                }
                if (request.AdditionalParameters.TryGetValue("ihracHaric", out object ihracHaricDeger))
                {
                    ihracHaric = bool.Parse((string)ihracHaricDeger);
                }
                if (request.AdditionalParameters.TryGetValue("zincirHaric", out object zincirHaricDeger))
                {
                    zincirHaric = bool.Parse((string)zincirHaricDeger);
                }
            }

            long userID = long.Parse(HttpContext.User.FindFirst("Id").Value);
            string kod = HttpContext.User.FindFirst("Chkod").Value;
            var carikod = logodb.LG_051_CLCARD.AsNoTracking().FirstOrDefault(x => x.Code == kod);

            using (SqlConnection logoConnection = new SqlConnection(_configuration.GetSection("ConnectionString:LogoConnection").Value))
            {
                IEnumerable<GrupBazindaFiyat> sorgu = Enumerable.Empty<GrupBazindaFiyat>();
                StringBuilder query = new StringBuilder(GrupBazindaOrtalamaSatisOncesi);

                if (ihracHaric == true)
                {
                    query.Append("AND ch.CODE NOT LIKE '120.İHR%'");
                }
                if (zincirHaric == true)
                {
                    query.Append("AND CH.SPECODE4 IN ('12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009')");
                }

                string lokasyon = "'12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009'";
                if (cariTip == "Bayiler")
                {
                    lokasyon = "'12-001'";
                }
                else if (cariTip == "ZincirMarketler")
                {
                    lokasyon = "'12-002'";
                }
                else if (cariTip == "OrtakAlicilar")
                {
                    lokasyon = "'12-003'";
                }
                else if (cariTip == "MerkezDagitim")
                {
                    lokasyon = "'12-004'";
                }
                else if (cariTip == "IhracatMusterileri")
                {
                    lokasyon = "'12-005'";
                }
                else if (cariTip == "Subeler")
                {
                    lokasyon = "'12-006'";
                }
                else if (cariTip == "YerelZincir")
                {
                    lokasyon = "'12-007'";
                }
                else if (cariTip == "KantinYemekhane")
                {
                    lokasyon = "'12-009'";
                }

                query.Append("AND CH.SPECODE4 IN (" + lokasyon + ")");
                query.Append("AND I.SPECODE IN ('102','103','104','10','11','12','13','21','31','41','51','61','71','81','82','91','92') GROUP BY  I.SPECODE ) AS DYNMQRY WHERE [NetSatisTutari]>0 ORDER BY DYNMQRY.URUNGRUBU ASC");

                sorgu = logoConnection.Query<GrupBazindaFiyat>(query.ToString(), new { T1 = baslangicTarihi, T2 = bitisTarihi });
                if (request.Search.Value != null)
                {
                    sorgu = sorgu.Where(_item => _item.URUNGRUBU.Contains(request.Search.Value));
                }
                var dataPage = sorgu.OrderByDescending(o => o.URUNGRUBU);/*.Skip(request.Start).Take(request.Length).ToList()*/
                var response = DataTablesResponse.Create(request, sorgu.Count(), sorgu.Count(), dataPage);
                return new DataTablesJsonResult(response);
            }
        }

        public IActionResult GetButunParcaOrtalama(IDataTablesRequest request)
        {
            DateTime? baslangicTarihi = null, bitisTarihi = null;
            string cariTip = null;
            bool ihracHaric = false, zincirHaric = false;


            if (request.AdditionalParameters != null)
            {
                if (request.AdditionalParameters.TryGetValue("baslangicTarihi", out object baslangicTarihiString))
                {
                    baslangicTarihi = DateTime.Parse(baslangicTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("bitisTarihi", out object bitisTarihiString))
                {
                    bitisTarihi = DateTime.Parse(bitisTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("cariTip", out object cariTipString))
                {
                    cariTip = cariTipString.ToString();
                }
                if (request.AdditionalParameters.TryGetValue("ihracHaric", out object ihracHaricDeger))
                {
                    ihracHaric = bool.Parse((string)ihracHaricDeger);
                }
                if (request.AdditionalParameters.TryGetValue("zincirHaric", out object zincirHaricDeger))
                {
                    zincirHaric = bool.Parse((string)zincirHaricDeger);
                }
            }

            long userID = long.Parse(HttpContext.User.FindFirst("Id").Value);
            string kod = HttpContext.User.FindFirst("Chkod").Value;
            var carikod = logodb.LG_051_CLCARD.AsNoTracking().FirstOrDefault(x => x.Code == kod);

            using (SqlConnection logoConnection = new SqlConnection(_configuration.GetSection("ConnectionString:LogoConnection").Value))
            {
                IEnumerable<ButunParcaOrtalama> sorgu = Enumerable.Empty<ButunParcaOrtalama>();
                StringBuilder query = new StringBuilder(ButunParcaOrtalamaSatisFiyatlari);

                if (ihracHaric == true)
                {
                    query.Append("AND ch.CODE NOT LIKE '120.İHR%'");
                }
                if (zincirHaric == true)
                {
                    query.Append("AND CH.SPECODE4 IN ('12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009')");
                }

                string lokasyon = "'12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009'";
                if (cariTip == "Bayiler")
                {
                    lokasyon = "'12-001'";
                }
                else if (cariTip == "ZincirMarketler")
                {
                    lokasyon = "'12-002'";
                }
                else if (cariTip == "OrtakAlicilar")
                {
                    lokasyon = "'12-003'";
                }
                else if (cariTip == "MerkezDagitim")
                {
                    lokasyon = "'12-004'";
                }
                else if (cariTip == "IhracatMusterileri")
                {
                    lokasyon = "'12-005'";
                }
                else if (cariTip == "Subeler")
                {
                    lokasyon = "'12-006'";
                }
                else if (cariTip == "YerelZincir")
                {
                    lokasyon = "'12-007'";
                }
                else if (cariTip == "KantinYemekhane")
                {
                    lokasyon = "'12-009'";
                }

                query.Append("AND CH.SPECODE4 IN (" + lokasyon + ")");
                query.Append("AND I.SPECODE IN ('102','103','104','10','11','12','13','21','31','41','51','61','71','81','82','91','92')GROUP BY I.CATEGORYNAME ) AS DYNMQRY WHERE [NetSatisTutari]>0 ORDER BY DYNMQRY.[UrunGrubu] ASC");

                sorgu = logoConnection.Query<ButunParcaOrtalama>(query.ToString(), new { T1 = baslangicTarihi, T2 = bitisTarihi });
                if (request.Search.Value != null)
                {
                    sorgu = sorgu.Where(_item => _item.UrunGrubu.Contains(request.Search.Value));
                }
                var dataPage = sorgu.OrderByDescending(o => o.UrunGrubu);/*.Skip(request.Start).Take(request.Length).ToList()*/
                var response = DataTablesResponse.Create(request, sorgu.Count(), sorgu.Count(), dataPage);
                return new DataTablesJsonResult(response);
            }
        }

        public IActionResult GetGraphic7liGrup(IDataTablesRequest request)
        {
            DateTime? baslangicTarihi = null, bitisTarihi = null;
            string cariTip = null;
            bool ihracHaric = false, zincirHaric = false;           

            if (request.AdditionalParameters != null)
            {
                if (request.AdditionalParameters.TryGetValue("baslangicTarihi", out object baslangicTarihiString))
                {
                    baslangicTarihi = DateTime.Parse(baslangicTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("bitisTarihi", out object bitisTarihiString))
                {
                    bitisTarihi = DateTime.Parse(bitisTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("cariTip", out object cariTipString))
                {
                    cariTip = cariTipString.ToString();
                }
                if (request.AdditionalParameters.TryGetValue("ihracHaric", out object ihracHaricDeger))
                {
                    ihracHaric = bool.Parse((string)ihracHaricDeger);
                }
                if (request.AdditionalParameters.TryGetValue("zincirHaric", out object zincirHaricDeger))
                {
                    zincirHaric = bool.Parse((string)zincirHaricDeger);
                }
            }

            long userID = long.Parse(HttpContext.User.FindFirst("Id").Value);
            string kod = HttpContext.User.FindFirst("Chkod").Value;
            var carikod = logodb.LG_051_CLCARD.AsNoTracking().FirstOrDefault(x => x.Code == kod);

            using (SqlConnection logoConnection = new SqlConnection(_configuration.GetSection("ConnectionString:LogoConnection").Value))
            {
                IEnumerable<Graphic7liGrup> sorgu = Enumerable.Empty<Graphic7liGrup>();
                StringBuilder query = new StringBuilder(Graphic7liGrup);

                if (ihracHaric == true)
                {
                    query.Append("AND ch.CODE NOT LIKE '120.İHR%'");
                }
                if (zincirHaric == true)
                {
                    query.Append("AND CH.SPECODE4 IN ('12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009')");
                }

                string lokasyon = "'12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009'";
                if (cariTip == "Bayiler")
                {
                    lokasyon = "'12-001'";
                }
                else if (cariTip == "ZincirMarketler")
                {
                    lokasyon = "'12-002'";
                }
                else if (cariTip == "OrtakAlicilar")
                {
                    lokasyon = "'12-003'";
                }
                else if (cariTip == "MerkezDagitim")
                {
                    lokasyon = "'12-004'";
                }
                else if (cariTip == "IhracatMusterileri")
                {
                    lokasyon = "'12-005'";
                }
                else if (cariTip == "Subeler")
                {
                    lokasyon = "'12-006'";
                }
                else if (cariTip == "YerelZincir")
                {
                    lokasyon = "'12-007'";
                }
                else if (cariTip == "KantinYemekhane")
                {
                    lokasyon = "'12-009'";
                }

                query.Append("AND CH.SPECODE4 IN (" + lokasyon + ")");
                query.Append("AND I.SPECODE IN ('102','103','104','10','11','12','13','21','31','41','51','61','71','81','82','91','92') GROUP BY I.KEYWORD1) AS DYNMQRY WHERE[NetSatisTutari] > 0 ORDER BY[Parça Grubu] ");
                query.Append("declare @toplam numeric(18, 2) set @toplam = (select SUM(NETMIKTAR) AS toplam from #graphicveri) declare @dokmebutun numeric(18,2) set @dokmebutun = (SELECT NETMIKTAR from #graphicveri WHERE PARCAGRUBU='1-DÖKME BÜTÜN')  declare @posetbutun numeric(18,2) set @posetbutun = (SELECT NETMIKTAR from #graphicveri WHERE PARCAGRUBU='2-POŞET BÜTÜN') declare @posetbutunihrac numeric(18,2) set @posetbutunihrac = (SELECT NETMIKTAR from #graphicveri WHERE PARCAGRUBU='3-POŞET BÜTÜN-İHRAÇ')  declare @cutup1 numeric(18,2) set @cutup1 = (SELECT NETMIKTAR from #graphicveri WHERE PARCAGRUBU='4-CUT-UP-1') declare @cutup2 numeric(18,2) set @cutup2 = (SELECT NETMIKTAR from #graphicveri WHERE PARCAGRUBU='5-CUT-UP-2')  declare @sakatat numeric(18,2) set @sakatat = (SELECT NETMIKTAR from #graphicveri WHERE PARCAGRUBU='6-SAKATAT')  declare @yanurun numeric(18,2) set @yanurun = (SELECT NETMIKTAR from #graphicveri WHERE PARCAGRUBU='7-YAN ÜRÜN') UPDATE #graphicveri SET ORAN= (NULLIF(@dokmebutun,0)/NULLIF(@toplam,0)) * 100 WHERE PARCAGRUBU='1-DÖKME BÜTÜN' UPDATE #graphicveri SET ORAN= (NULLIF(@posetbutun,0)/NULLIF(@toplam,0)) * 100 WHERE PARCAGRUBU='2-POŞET BÜTÜN' UPDATE #graphicveri SET ORAN= (NULLIF(@posetbutunihrac,0)/NULLIF(@toplam,0)) * 100 WHERE PARCAGRUBU='3-POŞET BÜTÜN-İHRAÇ' UPDATE #graphicveri SET ORAN= (NULLIF(@cutup1,0)/NULLIF(@toplam,0)) * 100 WHERE PARCAGRUBU='4-CUT-UP-1' UPDATE #graphicveri SET ORAN= (NULLIF(@cutup2,0)/NULLIF(@toplam,0)) * 100 WHERE PARCAGRUBU='5-CUT-UP-2' UPDATE #graphicveri SET ORAN= (NULLIF(@sakatat,0)/NULLIF(@toplam,0)) * 100 WHERE PARCAGRUBU='6-SAKATAT' UPDATE #graphicveri SET ORAN= (NULLIF(@yanurun,0)/NULLIF(@toplam,0)) * 100 WHERE PARCAGRUBU='7-YAN ÜRÜN' SELECT PARCAGRUBU, NETMIKTAR, NETSATISTUTARI, NETFIYAT, ORAN from #graphicveri ORDER BY CASE WHEN PARCAGRUBU='1-DÖKME BÜTÜN' THEN 1 WHEN PARCAGRUBU = '2-POŞET BÜTÜN' THEN 2 WHEN PARCAGRUBU = '3-POŞET BÜTÜN-İHRAÇ' THEN 3 WHEN PARCAGRUBU = '4-CUT-UP-1' THEN 4 WHEN PARCAGRUBU = '5-CUT-UP-2' THEN 5 WHEN PARCAGRUBU = '6-SAKATAT' THEN 6 WHEN PARCAGRUBU = '7-YAN ÜRÜN' THEN 7 END drop table #graphicveri");

                sorgu = logoConnection.Query<Graphic7liGrup>(query.ToString(), new { T1 = baslangicTarihi, T2 = bitisTarihi });
                if (request.Search.Value != null)
                {
                    sorgu = sorgu.Where(_item => _item.PARCAGRUBU.Contains(request.Search.Value));
                }
                var dataPage = sorgu.OrderByDescending(o => o.PARCAGRUBU);/*.Skip(request.Start).Take(request.Length).ToList()*/
                var response = DataTablesResponse.Create(request, sorgu.Count(), sorgu.Count(), dataPage);
                return new DataTablesJsonResult(response);
            }
        }

        public IActionResult GetGenelOrtalamaFiyat(IDataTablesRequest request)
        {
            DateTime? baslangicTarihi = null, bitisTarihi = null;
            string cariTip = null;
            bool ihracHaric = false, zincirHaric = false;

            if (request.AdditionalParameters != null)
            {
                if (request.AdditionalParameters.TryGetValue("baslangicTarihi", out object baslangicTarihiString))
                {
                    baslangicTarihi = DateTime.Parse(baslangicTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("bitisTarihi", out object bitisTarihiString))
                {
                    bitisTarihi = DateTime.Parse(bitisTarihiString.ToString());
                }
                if (request.AdditionalParameters.TryGetValue("cariTip", out object cariTipString))
                {
                    cariTip = cariTipString.ToString();
                }
                if (request.AdditionalParameters.TryGetValue("ihracHaric", out object ihracHaricDeger))
                {
                    ihracHaric = bool.Parse((string)ihracHaricDeger);
                }
                if (request.AdditionalParameters.TryGetValue("zincirHaric", out object zincirHaricDeger))
                {
                    zincirHaric = bool.Parse((string)zincirHaricDeger);
                }
            }
             
            long userID = long.Parse(HttpContext.User.FindFirst("Id").Value);
            string kod = HttpContext.User.FindFirst("Chkod").Value; 
            var carikod = logodb.LG_051_CLCARD.AsNoTracking().FirstOrDefault(x => x.Code == kod);

            using (SqlConnection logoConnection = new SqlConnection(_configuration.GetSection("ConnectionString:LogoConnection").Value))
            {
                IEnumerable<GenelOrtalamaFiyat> sorgu = Enumerable.Empty<GenelOrtalamaFiyat>();
                StringBuilder query = new StringBuilder(GenelOrtalamaFiyat);

                if (ihracHaric == true)
                {
                    query.Append("AND ch.CODE NOT LIKE '120.İHR%'");
                }
                if (zincirHaric == true)
                {
                    query.Append("AND CH.SPECODE4 IN ('12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009')");
                }

                string lokasyon = "'12-001','12-001-0','12-003','12-004','12-005','12-006','12-007','12-009'";
                if (cariTip == "Bayiler")
                {
                    lokasyon = "'12-001'";
                }
                else if (cariTip == "ZincirMarketler")
                {
                    lokasyon = "'12-002'";
                }
                else if (cariTip == "OrtakAlicilar")
                {
                    lokasyon = "'12-003'";
                }
                else if (cariTip == "MerkezDagitim")
                {
                    lokasyon = "'12-004'";
                }
                else if (cariTip == "IhracatMusterileri")
                {
                    lokasyon = "'12-005'";
                }
                else if (cariTip == "Subeler")
                {
                    lokasyon = "'12-006'";
                }
                else if (cariTip == "YerelZincir")
                {
                    lokasyon = "'12-007'";
                }
                else if (cariTip == "KantinYemekhane")
                {
                    lokasyon = "'12-009'";
                }

                query.Append("AND CH.SPECODE4 IN (" + lokasyon + ")");
                query.Append("AND I.CATEGORYNAME IN('BÜTÜN PİLİÇ','PARÇA ÜRÜN')");
                query.Append("AND I.SPECODE IN ('102','103','104','10','11','12','13','21','31','41','51','61','71','81','82','91','92') GROUP BY  I.CARDTYPE,L.DATE_) AS DYNMQRY WHERE [NetSatisTutari]>0 ORDER BY TARIH ");
                

                sorgu = logoConnection.Query<GenelOrtalamaFiyat>(query.ToString(), new { T1 = baslangicTarihi, T2 = bitisTarihi });
                
                var dataPage = sorgu.OrderByDescending(o => o.TARIH);/*.Skip(request.Start).Take(request.Length).ToList()*/
                var response = DataTablesResponse.Create(request, sorgu.Count(), sorgu.Count(), dataPage);
                return new DataTablesJsonResult(response);
            }
        }

        public IActionResult GetUretimIzle(IDataTablesRequest request)
        {
            var data = db.UretimIzleTamListe.AsNoTracking();
            IQueryable<UretimIzleTamListe> filteredData = data;
            if (request.Search.Value != null)
            {
                filteredData = filteredData.Where(_item => _item.ISIM.Contains(request.Search.Value) || _item.KOD.Contains(request.Search.Value));
            }

            var dataPage = filteredData.OrderByDescending(o => o.ISIM).Skip(request.Start).Take(request.Length);
            var response =  DataTablesResponse.Create(request, data.Count(), filteredData.Count(), dataPage);
            return new DataTablesJsonResult(response);
        }

        [HttpGet("fiyat-endeksi", Name = "FiyatEndeksi")]
        public IActionResult FiyatEndeksi()
        {
            return View();
        }

        [HttpGet("uretim-izle", Name = "UretimIzle")]
        public IActionResult UretimIzle()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
