$image = "registry.cn-hangzhou.aliyuncs.com/ql361/demo"  
  
# get timestamp for the tag  
$timestamp = Get-Date -UFormat "%Y%m%d%H%M$S"

# join tag 
$tag = "{0}:{1}" -f $image, $timestamp

# build image  
Write-Host $tag
docker build -t $tag .  
  
# push to dockerhub  
docker login  

# sudo docker login -u username -p password  
docker push $tag 

# remove dangling images  
# docker system prune -f