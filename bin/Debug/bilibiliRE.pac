function FindProxyForURL(url, host) {
      
      if (shExpMatch(url,"*://www.bilibili.com/*"))      
 
		{return "PROXY 127.255.255.253; DIRECT";}

      if (shExpMatch(url, "*://interface.bilibili.com/*"))    
  
	        {return "PROXY 127.255.255.253; DIRECT";}

      if (shExpMatch(url, "*://bilibili.kankanews.com/*"))    
  
	        {return "PROXY 127.255.255.253; DIRECT";}

      if (shExpMatch(url, "*://www.bilibili.tv/*"))    
  
	        {return "PROXY 127.255.255.253; DIRECT";}
 
            
   }