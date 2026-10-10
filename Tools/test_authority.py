import json
import sys
from pathlib import Path
import pytest
sys.path.insert(0,str(Path(__file__).parent))
from show_authority import Registry,ShowAuthority,KEYS,wire_roster

class FakeNet:
    client_no=99
    def __init__(self): self.variables={};self.rpcs=[]
    def set_global_variable(self,n,v): self.variables[n]=v
    def rpc(self,*a):self.rpcs.append(a)

def test_ten_unique_slots_and_keys():
    r=Registry();assert len(set(KEYS))==10
    assert [r.join('device'+str(i),i,100) for i in range(10)]==list(range(10))
    assert r.join('overflow',11,100)==-1
    assert r.join('device3',20,105)==3

def test_full_roster_fits_wire_limit_without_losing_identity():
    r=Registry()
    for i in range(10):r.join(('device-'+str(i)+'-').ljust(64,'x'),999999+i,100)
    records=r.records(101);encoded=wire_roster(records)
    assert len(encoded.encode('utf-8'))<=1024
    decoded=json.loads(encoded);assert decoded['v']==2 and len(decoded['p'])==10
    assert [p['d'] for p in decoded['p']]==[p['deviceId'] for p in records]
    assert len({p['s'] for p in decoded['p']})==10

def test_reconnect_reserved_slot_and_adaptive_presence():
    r=Registry();r.join('a',1,100)
    assert r.records(107)[0]['connected'] is False
    assert r.join('a',19,108)==0
    assert r.records(109)[0]['clientNo']==19
    assert sum(x['connected'] for x in r.records(109))==1

def test_full_cast_cannot_force_start_missing_clients():
    a=ShowAuthority(FakeNet(),mode='FullCast')
    with pytest.raises(ValueError):a.command('force_start')

def test_reset_increments_run_and_clears_epoch():
    a=ShowAuthority(FakeNet());a.command('seek',85);assert a.snapshot()['phase']=='Running'
    old=a.run;a.command('reset');assert a.run>old and a.epoch==0 and a.phase=='Ready'

def test_serialization_is_atomic():
    a=ShowAuthority(FakeNet());a.command('seek',85)
    s=json.loads(a.net.variables['ll.show']);assert s['epoch']>0 and s['duration']==121
    assert s['runId']==a.run and s['revision']==a.revision and s['protocol']==8

@pytest.mark.parametrize('value',[-1,122,float('nan'),float('inf')])
def test_invalid_seek_rejected(value):
    with pytest.raises(ValueError):ShowAuthority(FakeNet()).command('seek',value)

def test_active_slots_cannot_be_released():
    a=ShowAuthority(FakeNet());a.command('seek',85)
    with pytest.raises(ValueError):a.command('release_absent')

def test_cue_table_and_vfx_coverage():
    cues=json.loads((Path(__file__).parents[1]/'Assets/LordLyrical/Resources/Cues.json').read_text())['cues']
    assert len({c['id'] for c in cues})==len(cues)
    assert [c['time'] for c in cues]==sorted(c['time'] for c in cues)
    assert cues[-1]['time']==121
    assert set(range(1,31))=={v for c in cues for v in c['vfx']}
    assert {0,21,26,39,59,63,69,81,90,98,111,121}<={c['time'] for c in cues}

def test_alignment_is_required_even_for_force_start():
    import time
    a=ShowAuthority(FakeNet());a.registry.join('headset',1,time.monotonic())
    for command in ('start','force_start'):
        with pytest.raises(ValueError,match='Align connected headsets'):
            a.command(command)
    assert a.phase=='Ready' and a.epoch==0

def test_aligned_start_keeps_music_epoch_after_seventeen_second_prelude():
    import time
    a=ShowAuthority(FakeNet());a.registry.join('headset',1,time.monotonic());a.registry.aligned['headset']=True
    before=time.monotonic();a.command('start');after=time.monotonic()
    assert before+17<=a.epoch<=after+17
    assert a.snapshot()['openingDuration']==17 and a.snapshot()['duration']==121
    assert a.phase=='Countdown'

def test_all_ten_physical_headsets_must_align():
    import time
    a=ShowAuthority(FakeNet(),mode='FullCast')
    for i in range(10):
        device='headset'+str(i);a.registry.join(device,i,time.monotonic());a.registry.aligned[device]=i!=9
    with pytest.raises(ValueError,match='10'):a.command('start')
    a.registry.aligned['headset9']=True;a.command('start')
    assert a.phase=='Countdown'

def test_alignment_uses_verified_sender_and_resets_when_client_reports_unaligned():
    class MappedNet(FakeNet):
        def get_device_id_from_client_no(self,sender):return {7:'real-headset'}.get(sender)
        def set_client_variable(self,*args):pass
    a=ShowAuthority(MappedNet())
    a.on_rpc(999,'ll.join',['aligned']);assert a.registry.slots==[None]*10
    a.on_rpc(7,'ll.join',['aligned']);assert a.registry.aligned=={'real-headset':True}
    a.on_rpc(7,'ll.join',['unaligned']);assert a.registry.aligned=={'real-headset':False}
