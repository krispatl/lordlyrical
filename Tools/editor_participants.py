"""Nine show-aware spectral test clients beside a real Unity Editor client."""
import argparse,math,time
from styly_netsync.client import net_sync_manager
from styly_netsync.types import client_transform_data,transform_data
p=argparse.ArgumentParser();p.add_argument('--clients',type=int,default=9);p.add_argument('--duration',type=float,default=150);p.add_argument('--room',default='lord-lyrical');a=p.parse_args()
clients=[net_sync_manager(server='tcp://127.0.0.1',room=a.room,auto_dispatch=False) for _ in range(a.clients)]
for c in clients:c.start()
began=time.monotonic();next_join=0
try:
 while time.monotonic()-began<a.duration:
  t=time.monotonic()-began
  for i,c in enumerate(clients):
   c.dispatch_pending_events()
   if not c.is_ready:continue
   if t>=next_join:c.rpc('ll.join',[])
   angle=i*2*math.pi/a.clients;x=math.sin(angle)*3;z=math.cos(angle)*2-1
   head=transform_data(pos_x=x,pos_y=1.7+math.sin(t+i)*.06,pos_z=z)
   left=transform_data(pos_x=x-.3,pos_y=1.2+math.sin(t*2+i)*.1,pos_z=z+.3)
   right=transform_data(pos_x=x+.3,pos_y=1.2,pos_z=z+.3)
   c.send_transform(client_transform_data(client_no=c.client_no,device_id=c.device_id,head=head,left_hand=left,right_hand=right))
  if t>=next_join:next_join=t+1
  time.sleep(.05)
finally:
 for c in clients:c.stop()
