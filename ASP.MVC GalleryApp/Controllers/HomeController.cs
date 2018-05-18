using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Lab5.Models;
using Image = System.Drawing.Image;



namespace Lab5.Controllers
{
    public class HomeController : Controller
    {
        
        [HttpGet]
        public ActionResult Index()
        {
            GalleryDBEntities gm = new GalleryDBEntities(); 
            var galleryList = from gallery in gm.GALLERY_LIST
                orderby gallery.Gallery_Number
                select gallery;

            ViewBag.Galleries = galleryList;
            return View("Index");
        }
        [HttpPost]
        ///<summary>
        /// Index View with login error notification
        /// </summary>
        public ActionResult Index(string errorMsg) 
        {
            ViewBag.Error = errorMsg;
            return Index();
        }    
        [HttpGet]
        public ActionResult GalleryDetails(string gName)
        {
            
            GalleryDBEntities gm = new GalleryDBEntities();
            var target = (from gallery in gm.GALLERY_LIST where gallery.Gallery_Name==gName select gallery).FirstOrDefault(); 
            var pictures = from picture in gm.PICTURE_LIST
                               where picture.Gallery_ID == target.ID
                               orderby picture.Picture_Number
                               select picture;
            ViewBag.Gallery = target;
            ViewBag.Imgs = pictures;
            return View();
        }    
        [HttpGet]
        public ActionResult PictureDetails(int picNum, int gID , string prevUrl)    
        {
            GalleryDBEntities gm = new GalleryDBEntities();
            List<int> validNums=new List<int>();
            var currGallery = (from gallery in gm.GALLERY_LIST
                where gallery.ID == gID
                select gallery).First();
            PICTURE_LIST img = (from picture in gm.PICTURE_LIST
                where picture.Picture_Number == picNum && picture.Gallery_ID==currGallery.ID
                select picture).FirstOrDefault();
            ViewBag.Picture = img;
            ViewBag.Gallery = currGallery;
            foreach (var pic in currGallery.PICTURE_LIST)
            {
                validNums.Add(pic.Picture_Number);
            }
            ViewBag.ValidINDs = validNums;
            Regex pattern = new  Regex(@"Upload");
            if (prevUrl == null)
            {
                ViewBag.PrevURL = Request.UrlReferrer;
            }
            else
            {
                if (!pattern.IsMatch(prevUrl))
                {
                    ViewBag.PrevURL = prevUrl;
                }
                else
                {
                    ViewBag.PrevURL = Url.Action("GalleryDetailsLogedIn", "Home", new {currGallery.Gallery_Name});
                }
                
            }
            return View();     
                        
        }  
        [HttpGet]
        public ActionResult IndexLoggedIn()
        {
            
            GalleryDBEntities gm = new GalleryDBEntities();
            var galleryList = from gallery in gm.GALLERY_LIST
                orderby gallery.Gallery_Number
                select gallery;
            ViewBag.Galleries = galleryList;
            return View("IndexLoggedIn");
        }    
        [HttpPost]
        public ActionResult LogIn(string login, string pass)
        {
            GalleryDBEntities gm = new GalleryDBEntities();
            try
            {
                var user = (from account in gm.Account
                    where account.Login == login & account.Password == pass
                    select account).First();
                
                    return IndexLoggedIn();
             }
            catch (Exception)
            {
                return Index("Invalid login / password!");
            }
        }    
        [HttpPost]
        public ActionResult ChangeGallery(ChangeAction action, int? galleryID, string newGallName)
        {
            
            GalleryDBEntities gm = new GalleryDBEntities();
            List<GALLERY_LIST> galleries = gm.GALLERY_LIST.ToList();
            if (action == ChangeAction.Create && galleryID == null)
            {
                int maxNum = 0;
                foreach (var gall in galleries)
                {
                    if (maxNum < gall.Gallery_Number)
                    {
                        maxNum = gall.Gallery_Number;
                    }

                }
                GALLERY_LIST newGall = new GALLERY_LIST { Gallery_Name = newGallName, Gallery_Number = maxNum+1};
                gm.GALLERY_LIST.Add(newGall);
                gm.SaveChanges();
               CreateGalleryFolder(gm.Entry(newGall).Entity.ID); 
            }
            GALLERY_LIST gallery = (from gall in gm.GALLERY_LIST where gall.ID == galleryID select gall).FirstOrDefault();
            GALLERY_LIST prevGallery;
            int targetInd, otherInd;
            switch (action)
            {
                case ChangeAction.Up:
                    targetInd = gallery.Gallery_Number;
                    otherInd = targetInd;
                    otherInd--;
                    prevGallery = (from galleryDown in galleries
                        where galleryDown.Gallery_Number == otherInd
                        select galleryDown).FirstOrDefault();
            
                    if (prevGallery != null)
                    {

                        gallery.Gallery_Number = otherInd;

                        prevGallery.Gallery_Number = targetInd;
                        gm.SaveChanges();
                    }
                    break;

               case ChangeAction.Down:   
                    targetInd = gallery.Gallery_Number;
                    otherInd = targetInd;
                    otherInd++;
                    prevGallery = (from galleryDown in galleries
                        where galleryDown.Gallery_Number == otherInd
                        select galleryDown).FirstOrDefault();

                    if (prevGallery != null)
                    {

                    gallery.Gallery_Number = otherInd;
                    
                    prevGallery.Gallery_Number = targetInd;
                    gm.SaveChanges();
                    }

                    break;
                case ChangeAction.Delete:
                    
                    if (gallery != null)
                    {
                        DeleteGalleryFolder(gallery.ID);
                        gm.Entry(gallery).State = EntityState.Deleted;
                        gm.SaveChanges();
                    }

                    break;
                case ChangeAction.Create:
                    
                    break;
            }

            return IndexLoggedIn();
        }
        private void CreateGalleryFolder(int galleryID)
        {
            Directory.CreateDirectory(Server.MapPath($"/Resources/PhotoStorage/{galleryID}"));
        }
        private void DeleteGalleryFolder(int galleryID)
        {
            DirectoryInfo di = new DirectoryInfo(Server.MapPath($"~/Resources/PhotoStorage/{galleryID}"));
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
            Directory.Delete(di.FullName);
        }
        [HttpGet]
        public ActionResult GalleryDetailsLogedIn(string gName)
        {
            List<string> imgSources = new List<string>();
            GalleryDBEntities gm = new GalleryDBEntities();
            var target = (from gallery in gm.GALLERY_LIST where gallery.Gallery_Name == gName select gallery).First(); //!could make some trouble!
            var pictures = from picture in gm.PICTURE_LIST
                orderby picture.Picture_Number
                where picture.Gallery_ID == target.ID
                select picture;
            ViewBag.Gallery = target;
            ViewBag.Imgs = pictures;
            
            return View("GalleryDetailsLogedIn");
        }
        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase upload,string tooltip, int gallID,string gName)
        {
            if (upload != null)
            {
                
                string fileName = Path.GetFileName(upload.FileName);
                string fullVpath = Server.MapPath($"~/Resources/PhotoStorage/{gallID}/" + fileName);
                upload.SaveAs(fullVpath);
                Bitmap mini= new Bitmap(Image.FromFile(fullVpath),200,150);
                string miniVpath = Server.MapPath($"~/Resources/PhotoStorage/{gallID}/" + "mini" + fileName);
                mini.Save(miniVpath);
                GalleryDBEntities gm =new GalleryDBEntities();
                int maxNum = 0;
                var pictures = from picture in gm.PICTURE_LIST where picture.Gallery_ID == gallID select picture;
                foreach (var pic in pictures)
                {
                    if (maxNum < pic.Picture_Number)
                    {
                        maxNum = pic.Picture_Number;
                    }

                }
                PICTURE_LIST newPic = new PICTURE_LIST
                {
                    Description = tooltip,
                    Full_Version = fileName,
                    Gallery_ID = gallID,
                    Mini_Version = "mini" + fileName,
                    Picture_Number = maxNum + 1
                };
                gm.PICTURE_LIST.Add(newPic);
                gm.SaveChanges();
            }
            return GalleryDetailsLogedIn(gName);
        }
        [HttpPost]
        public ActionResult ChangePicture(ChangeAction action, int? picID, string gName, int gID)
        {

            GalleryDBEntities gm = new GalleryDBEntities();
            List<PICTURE_LIST> pictures = (from pic in gm.PICTURE_LIST where pic.Gallery_ID==gID select pic).ToList();
            PICTURE_LIST picture = (from pic in gm.PICTURE_LIST where pic.ID == picID select pic).FirstOrDefault();
            PICTURE_LIST prevPic;
            int targetInd, otherInd;
            switch (action)
            {
                case ChangeAction.Up:
                    targetInd = picture.Picture_Number;
                    otherInd = targetInd;
                    otherInd--;
                    prevPic = (from otherPic in pictures
                        where otherPic.Picture_Number == otherInd
                        select otherPic).FirstOrDefault();
                    if (prevPic != null)
                    {

                        picture.Picture_Number = otherInd;

                        prevPic.Picture_Number = targetInd;
                        gm.SaveChanges();
                    }
                    break;



                    
                case ChangeAction.Down:
                    targetInd = picture.Picture_Number;
                    otherInd = targetInd;
                    otherInd++;
                    prevPic = (from otherPic in pictures
                        where otherPic.Picture_Number == otherInd
                        select otherPic).FirstOrDefault();
                    if (prevPic != null)
                    {

                        picture.Picture_Number = otherInd;

                        prevPic.Picture_Number = targetInd;
                        gm.SaveChanges();
                    }
                    break;



                    
                case ChangeAction.Delete:

                    if (picture != null)
                    {
                        DeletePicture(Server.MapPath($"~/Resources/PhotoStorage/{gID}/"),picture.Full_Version);
                        gm.Entry(picture).State = EntityState.Deleted;
                        gm.SaveChanges();
                    }

                    break;
                case ChangeAction.Create:

                    break;
            }

            return GalleryDetailsLogedIn(gName);
        }       
        private void DeletePicture(string path, string filename)
        {
            System.IO.File.Delete(path + filename);
            System.IO.File.Delete(path+"mini"+filename);
        }
    }

    public enum ChangeAction { Up,Down,Delete, Create}
    
}  

   

    




            
            
       
                    
                
                    
                    
               
                
           

        

        

        

       
        

       

       
            








        
            
            
            
            
            
              
       
  
           
            

        

       

        

        
        

      

       