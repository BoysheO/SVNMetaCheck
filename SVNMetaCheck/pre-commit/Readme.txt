实践中，使用visualSVN作服务器时，有以下几个坑

1.默认不支持中文
    虽然可以改服务器配置，为了方便期间直接不用中文返错误消息
2.默认使用https
    会导致svn cat的时候由于无法访问服务导致cat失败。所以要配成http。理论上是可以通过echo p | svn ls https://xxx来信任证书的，但实际上运行时windows的权限是NetworkServices，操作不了信任。
3.要引用此svn库的svn库都要修改引用url
    无解，手动修改
    
