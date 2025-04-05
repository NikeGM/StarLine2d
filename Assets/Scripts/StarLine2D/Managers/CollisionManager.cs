using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarLine2D.Controllers;
using StarLine2D.Factories;

namespace StarLine2D.Managers
{
    public interface ICollisionParticipant
    {
        IEnumerable<CellController> DesiredCells { get; }
        float Mass { get; }
        bool IsObstacle { get; }
        int OnDamage(int dmg);
        CellController PositionCell { get; set; }
    }

    public class CollisionData
    {
        public readonly CellController cell;
        public readonly List<ICollisionParticipant> participants;

        public CollisionData(CellController cell, List<ICollisionParticipant> participants)
        {
            this.cell = cell;
            this.participants = participants;
        }
    }

    public class CollisionManager : MonoBehaviour
    {
        [SerializeField] private ShipFactory shipFactory;
        [SerializeField] private AsteroidFactory asteroidFactory;
        [SerializeField] private ObstacleFactory obstacleFactory;
        [SerializeField] private FieldController field;

        private readonly List<CollisionData> _collisions = new();

        private void Awake()
        {
            if (!shipFactory) Debug.LogError($"[{name}] ShipFactory is not assigned.");
            if (!asteroidFactory) Debug.LogError($"[{name}] AsteroidFactory is not assigned.");
            if (!obstacleFactory) Debug.LogError($"[{name}] ObstacleFactory is not assigned.");
            if (!field) Debug.LogError($"[{name}] FieldController is not assigned.");
        }

        public void CollectPotentialCollisions()
        {
            _collisions.Clear();
            var ships = shipFactory.GetSpawnedShips().OfType<ICollisionParticipant>();
            var asteroids = asteroidFactory.GetSpawnedAsteroids().OfType<ICollisionParticipant>();
            var obstacles = obstacleFactory.GetSpawnedObstacles().OfType<ICollisionParticipant>();

            var allParticipants = new List<ICollisionParticipant>();
            allParticipants.AddRange(ships);
            allParticipants.AddRange(asteroids);
            allParticipants.AddRange(obstacles);

            var dict = new Dictionary<CellController, List<ICollisionParticipant>>();

            foreach (var participant in allParticipants)
            {
                var desiredCells = participant.DesiredCells;
                if (desiredCells == null) continue;
                foreach (var cell in desiredCells)
                {
                    if (!cell) continue;
                    if (!dict.ContainsKey(cell)) dict[cell] = new List<ICollisionParticipant>();
                    dict[cell].Add(participant);
                }
            }

            foreach (var kvp in dict)
            {
                var cell = kvp.Key;
                var list = kvp.Value;
                if (list.Count > 1) _collisions.Add(new CollisionData(cell, list));
            }

            Debug.Log($"[CollisionManager] CollectPotentialCollisions: найдено {_collisions.Count} коллизий.");
        }

        public void ProcessCollisions()
        {
            if (_collisions.Count == 0)
            {
                Debug.Log("[CollisionManager] Нет коллизий для обработки.");
                return;
            }

            foreach (var collision in _collisions)
            {
                var cell = collision.cell;
                var participants = collision.participants;
                var hasObstacle = participants.Any(p => p.IsObstacle);
                var partsInfo = string.Join(", ", participants.Select(p => 
                    $"{p.GetType().Name}(mass={p.Mass}, obstacle={p.IsObstacle})"));
                Debug.Log($"[CollisionManager] === Коллизия в клетке [{cell.name}] ===\n" +
                          $"Участников={participants.Count}. Список: [{partsInfo}].\n" +
                          $"Obstacle? {hasObstacle}");

                if (!hasObstacle)
                {
                    Debug.Log("[CollisionManager] Нет препятствия => урон = сумма масс остальных объектов.");
                    foreach (var p in participants)
                    {
                        float sumOthers = 0f;
                        foreach (var other in participants)
                        {
                            if (other != p) sumOthers += other.Mass;
                        }
                        var damage = Mathf.RoundToInt(sumOthers);
                        Debug.Log($"    => {p.GetType().Name} получает урон={damage}");
                        var realDamage = p.OnDamage(damage);
                        Debug.Log($"    => Вызван OnDamage({damage}), реальный урон={realDamage}");
                    }
                }
                else
                {
                    Debug.Log("[CollisionManager] Есть препятствие => каждый НЕ препятствие получает урон = своя масса.");
                    foreach (var p in participants)
                    {
                        Debug.Log($"   Проверяем {p.GetType().Name} (mass={p.Mass}, IsObstacle={p.IsObstacle})");
                        if (!p.IsObstacle)
                        {
                            var dmg = Mathf.RoundToInt(p.Mass);
                            Debug.Log($"      => Наносим урон={dmg} этому объекту...");
                            var realDamage = p.OnDamage(dmg);
                            Debug.Log($"      => OnDamage({dmg}) вернул {realDamage} (объект: {p.GetType().Name}, mass={p.Mass})");
                        }
                        else
                        {
                            Debug.Log("      => Это препятствие, урон не наносим.");
                        }
                    }
                }

                var alive = participants.Where(p => p != null && p.PositionCell != null).ToList();
                if (alive.Count == 0)
                {
                    Debug.Log("    => Все погибли в этом столкновении. Клетка остаётся пустой.");
                    continue;
                }

                if (!hasObstacle)
                {
                    var winner = alive.OrderByDescending(p => p.Mass).First();
                    Debug.Log($"    => Победитель: {winner.GetType().Name} массой={winner.Mass} (ставим на {cell.name})");
                    winner.PositionCell = cell;
                    var losers = alive.Where(p => p != winner).ToList();
                    var neighbors = field.GetNeighbors(cell, 1);
                    var freeNeighbors = new List<CellController>();
                    foreach (var n in neighbors)
                    {
                        var occupied = alive.Any(a => a.PositionCell == n);
                        if (!occupied) freeNeighbors.Add(n);
                    }

                    var idx = 0;
                    foreach (var l in losers)
                    {
                        if (idx < freeNeighbors.Count)
                        {
                            Debug.Log($"    => Лузер {l.GetType().Name} отправляется в клетку {freeNeighbors[idx].name}");
                            l.PositionCell = freeNeighbors[idx];
                            idx++;
                        }
                        else
                        {
                            Debug.Log($"    => Для {l.GetType().Name} не хватило соседних клеток, остаётся без клетки.");
                            l.PositionCell = null;
                        }
                    }
                }
                else
                {
                    var survivors = alive.Where(p => !p.IsObstacle).ToList();
                    foreach (var s in survivors)
                    {
                        if (s is AsteroidController asteroid)
                        {
                            Debug.Log($"[CollisionManager] Астероид {asteroid.name} выжил при столкновении с препятствием!");
                            asteroid.RevertToOldCellAndReverseDirection();
                        }
                        else
                        {
                            Debug.Log($"[CollisionManager] Корабль {s.GetType().Name} выжил при столкновении с препятствием, " +
                                      $"остаётся на клетке {s.PositionCell?.name}.");
                        }
                    }
                }
            }
        }

        public void CheckShipCollisions()
        {
        }

        public void CleanupAsteroids()
        {
        }

        public void CleanupShips()
        {
        }
    }
}
