using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 点选验证码内置英文常用词表 / Built-in pool of common English words for the click captcha word mode.
    /// </summary>
    /// <remarks>
    ///  Description:    点选验证码内置英文常用词表
    ///                  约 400 个常用英文单词（3~7 个字母、全小写），供 WordClickVerificationCode 默认取词；
    ///                  使用方可经 ClickVerificationCodeOptions.EnglishWordPool 覆盖。
    ///  Description:    About 400 common English words (3-7 letters, all lowercase) used by
    ///  WordClickVerificationCode by default; consumers can override via
    ///  ClickVerificationCodeOptions.EnglishWordPool.
    ///
    /// </remarks>
    internal static class EnglishWordPool
    {
        /// <summary>
        /// 内置常用英文词表（逗号分隔，约 400 词，全小写） /
        /// The built-in pool of common English words, comma-separated (~400 words, all lowercase).
        /// </summary>
        internal const string CommonEnglishWords = "time,year,way,day,man,thing,woman,life,child,world,school,state,family,student,group,country,problem,hand,place,case,week,work,night,point,home,water,room,mother,area,money,story,fact,month,study,book,eye,job,word,issue,side,kind,head,house,friend,father,power,hour,game,line,member,law,car,city,name,team,minute,idea,kid,body,back,parent,face,level,office,door,health,person,art,war,party,result,change,morning,reason,girl,guy,moment,air,teacher,force,foot,boy,age,music,market,sense,nation,plan,college,object,death,effect,class,control,care,field,role,effort,rate,heart,drug,show,leader,light,voice,wife,police,mind,price,report,view,town,road,value,action,model,season,society,tax,player,record,paper,space,ground,form,event,matter,center,couple,site,project,beauty,star,table,court,cost,figure,street,image,phone,data,news,test,movie,north,love,step,baby,type,film,tree,source,truth,song,half,sea,king,sun,ice,fire,wind,wood,dog,cat,bird,fish,food,meat,rice,milk,egg,tea,cup,box,bag,bed,desk,chair,wall,river,lake,hill,stone,cloud,rain,snow,storm,beach,train,plane,ship,boat,bike,bus,bridge,shop,store,farm,bank,hotel,garden,window,island,station,airport,travel,trip,tour,walk,run,jump,swim,read,write,sing,dance,draw,play,open,close,start,stop,move,turn,help,call,ask,tell,talk,say,speak,hear,see,look,watch,find,give,take,make,know,think,learn,teach,live,eat,drink,sleep,wake,rest,wait,meet,send,buy,sell,pay,big,small,old,new,young,good,bad,hot,cold,warm,cool,dry,wet,rich,poor,fast,slow,high,low,long,short,tall,fat,thin,strong,happy,sad,angry,calm,mean,nice,fine,real,true,full,empty,clean,dirty,dark,bright,soft,hard,heavy,easy,about,above,under,again,always,never,often,soon,now,then,here,there,where,when,what,why,how,all,any,some,many,much,more,most,few,both,each,every,other,same,own,one,two,three,four,five,six,ten,plant,leaf,root,seed,flower,grass,tiger,horse,sheep,mouse,snake,whale,shark,eagle,lion,bear,wolf,frog,duck,goose,pig,cow,goat,blue,green,black,white,red,pink,gray,brown,yellow,purple,orange,banana,cherry,tomato,potato,carrot,onion,salad,soup,cake,pie,honey,candy,cookie,cream,juice,dream,smile,laugh,sweet,sugar,salt,bread,apple,grape,lemon,peach,mango,melon,hundred";

        /// <summary>
        /// 内置词表的数组形式（类初始化时切分一次，供默认取词复用） /
        /// The built-in pool as an array (split once at type init, reused by the default seeding).
        /// </summary>
        internal static readonly string[] DefaultWords = CommonEnglishWords.Split(',');
    }
}
