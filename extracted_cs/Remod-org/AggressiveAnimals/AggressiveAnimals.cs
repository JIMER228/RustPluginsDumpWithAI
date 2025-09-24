#region License (GPL v2)
/*
    Aggressive Animals - Bears and wolves no longer run away
    Copyright (c) 2023 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v2)

namespace Oxide.Plugins
{
    [Info("AggressiveAnimals", "RFC1920", "1.0.3")]
    [Description("Bears, polar bears, and wolves attack when attacked instead of running away")]
    internal class AggressiveAnimals : RustPlugin
    {
        bool debug = true;
        private object OnEntityTakeDamage(BaseAnimalNPC entity, HitInfo info)
        {
            string nom = entity?.GetType()?.Name;
            if ((nom == "Polarbear" || nom == "Bear" || nom == "Wolf") && entity.health > 20)
            {
                if (entity?.brain == null) { return null; }
                entity?.brain?.states.Remove(AIState.Flee);
                entity?.brain?.states.Remove(AIState.Cover);
                if (!entity.brain.states.ContainsKey(AIState.Attack))
                {
                    entity?.brain?.AddState(new AnimalBrain.AttackState());
                }
                entity?.brain?.states[AIState.Attack].StateEnter(entity.brain, entity);
            }
            return null;
        }
    }
}
