using UnityEngine;
using TMPro;

public class PlayerMove : MonoBehaviour
{
    public Rigidbody2D playerbode;
    public CircleCollider2D PlayerSansGround;
    public Transform PlayerSansWall;
    public Animator animator;
    public TMP_Text oxygenText;

    public LayerMask Ground;
    public LayerMask Wall;

    
    [Header("Movement")]///Переменные для Move
    [Tooltip("Скрость в направлении бега")]
    public float moveX;
    public float moveSpeed = 5f;

    [Header("Jump")]///Переменные для Jump
    public float jumpForce = 5f;
    public float jumpHoldForce = 0.15f;
    public float maxJumpHoldTime = 0.3f;
    private float jumpDirection;
    public float jumpHorizontalForce = 5f;
    private float jumpHoldTimer;
    public float normalGravity = 1f;
    public float jumpGravity = 1.5f;


    [Header("AirControl")]///Переменные для AirControl
    public float airDrag = 8f;

    public float minAirSpeed = 1.5f;
    public float airBrakeForce = 10f;

    
    [Header("Directionl")]///Переменные для Direction
    public int facingDirection = 1;
    private Vector3 originalScale;

    
    [Header("Checking")]///Переменные для Checking
    public float sensGroundRadius = 0.12f;
    public float sensWallRadius = 0.12f;
    public bool onGround;
    public bool atw;

    
    [Header("WallSlide")]///Переменные для WallSlide
    private float wallStickTimer;
    public float wallStickTime = 0.2f;
    public float wallSlideSpeed = 2f;

    
    [Header("WallJump")]///Переменные для WallJump
    public float wallJumpForceX = 2f;
    public float wallJumpForceY = 2f;

    private bool jetpackLocked;
    private float jetpackLockTimer;

    public float jetpackDisableTime = 0.2f;
    public float wallJumpDisableTime = 0.2f;

    private bool isWallJumping;
    private float wallJumpTimer;

    
    [Header("Jetpack")]///Переменные для Jetpack
    public float jetpackForce = 15f;
    public float maxJetpackSpeed = 3f;
    public float jetpackAirAcceleration = 10f;
    public float jetpackGravity = 0.3f;
    public float oxygen = 100f;
    public float maxOxygen = 100f;
    public float oxygenUseSpeed = 1f;
    public float oxygenRecoverSpeed = 0.5f;
    ////////////////////////
    
    [Space(10)]
    SpriteRenderer sr;


    void Start()
    {
        playerbode.gravityScale = normalGravity;

        oxygen = maxOxygen;

        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;
    }
    void Update()
    {
        HandleGravity();
        Move();
        WallJump();
        WallJumpTimer();
        Jump();
        Direction();
        CheckingGround();
        CheckingWall();
        AirControl();
        WallSlide();
        Jetpack();

        UpdateAnimator();
    }

    void HandleGravity()
    {
        // Jetpack важнее всего
        if (!onGround && !jetpackLocked && Input.GetButton("Jetpack") && oxygen > 0)
        {
            playerbode.gravityScale = jetpackGravity;
        }

        // Удержание прыжка
        else if (Input.GetButton("Jump") &&
                 jumpHoldTimer > 0 &&
                 playerbode.linearVelocity.y > 0)
        {
            playerbode.gravityScale = jumpGravity;
        }

        // Обычная гравитация
        else
        {
            playerbode.gravityScale = normalGravity;
        }
    }
    void Move()////Отвечает за движение 
    {
        if (onGround == true)
        {
            moveX = Input.GetAxis("Horizontal") * moveSpeed;
            Vector2 velocity = playerbode.linearVelocity;
            velocity.x = moveX;
            playerbode.linearVelocity = velocity;
        }
    }
    void Jump()////Отвечает за прыжок 
    {
        // НАЧАЛО ПРЫЖКА
        if (Input.GetButtonDown("Jump") && onGround)
        {
            jumpHoldTimer = maxJumpHoldTime;

            // направление прыжка
            if (moveX != 0)
            {
                jumpDirection = Mathf.Sign(moveX);
            }
            else
            {
                jumpDirection = 0;
            }

            // импульс
            playerbode.linearVelocity = new Vector2(
                jumpDirection * jumpHorizontalForce,
                jumpForce
            );
        }

        // удержание прыжка
        if (Input.GetButton("Jump") &&
            jumpHoldTimer > 0 &&
            playerbode.linearVelocity.y > 0)
        {
            jumpHoldTimer -= Time.deltaTime;
        }

        // отпустили кнопку
        if (Input.GetButtonUp("Jump"))
        {
            jumpHoldTimer = 0;
        }
    }
    void WallJumpTimer()////Отвечает за задержку прилепания  после прыжка после прыжка от стеныц 
    {
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;

            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }

        // Блокировка Jetpack после wall jump
        if (jetpackLocked)
        {
            jetpackLockTimer -= Time.deltaTime;

            if (jetpackLockTimer <= 0)
            {
                jetpackLocked = false;
            }
        }
    }
    void AirControl()////Отвечает за контроль дальности прыжка в воздухе 
    {
        if (!onGround)
        {
            float input = Input.GetAxis("Horizontal");
            float velX = playerbode.linearVelocity.x;

            // Игрок НЕ нажимает направление
            if (Mathf.Abs(input) < 0.1f)
            {
                float newX = Mathf.MoveTowards(
                    velX,
                    0,
                    airDrag * Time.deltaTime
                );

                playerbode.linearVelocity = new Vector2(
                    newX,
                    playerbode.linearVelocity.y
                );
            }

            // Игрок жмёт в противоположную сторону
            else if (Mathf.Sign(input) != Mathf.Sign(velX))
            {
                float newX = Mathf.MoveTowards(
                    velX,
                    0,
                    airBrakeForce * Time.deltaTime
                );

                playerbode.linearVelocity = new Vector2(
                    newX,
                    playerbode.linearVelocity.y
                );
            }
        }
    }
    void Direction()////Отвечает за опредиление напровления взгляда персонажа 
    {

        sr.flipX = false;// Сбрасываем flipX после WallSlide,
                         // чтобы персонаж снова смотрел по напровлению Scale

        if (moveX > 0)
        {
            facingDirection = 1;
        }
        else if (moveX < 0)
        {
            facingDirection = -1;
        }

        ///Отвечает за переварот спрайта персонажа в зависемости от значения facingDirection
        if (transform.localScale.x != originalScale.x * facingDirection)
        {
            transform.localScale = new Vector3(
                originalScale.x * facingDirection,
                originalScale.y,
                originalScale.z
            );
        }
    }
    void CheckingGround()////Отвечает за опередиления стоитле персонаж на земле 
    {
        var offset3D = new Vector3(PlayerSansGround.offset.x, PlayerSansGround.offset.y, 0);
        onGround = Physics2D.OverlapCircle(PlayerSansGround.transform.position + offset3D, PlayerSansGround.radius, Ground);
    }
    void CheckingWall()////Отвечает за опередиления стоитле персонаж у стены 
    {
        atw = Physics2D.OverlapCircle(PlayerSansWall.position, sensWallRadius, Wall);
    }
    void WallSlide()////Отвечает за слай по стене 
    {
        float input = Input.GetAxis("Horizontal");

        if (atw && !onGround && !isWallJumping)
        {
            if (wallStickTimer > 0)
            {
                wallStickTimer -= Time.deltaTime;

                playerbode.linearVelocity = new Vector2(0, 0);
            }
            else
            {
                if (playerbode.linearVelocity.y < -wallSlideSpeed)
                {
                    playerbode.linearVelocity = new Vector2(
                        playerbode.linearVelocity.x,
                        -wallSlideSpeed
                    );
                }
            }
        }
        else
        {
            wallStickTimer = wallStickTime;
        }
    }
    void WallJump()////Отвечает за прыжки от стен 
    {

        if (atw && !onGround && Input.GetButtonDown("Jump"))
        {
            moveX = 0;

            int jumpDirection = -facingDirection;

            facingDirection = jumpDirection;

            playerbode.linearVelocity = new Vector2(
                jumpDirection * wallJumpForceX,
                wallJumpForceY
            );

            isWallJumping = true;
            wallJumpTimer = wallJumpDisableTime;

            jetpackLocked = true;
            jetpackLockTimer = jetpackDisableTime;
        }
    }
    void Jetpack() //// Отвечает за управление джетпаком
    {
        if (!onGround && !jetpackLocked && Input.GetButton("Jetpack") && oxygen > 0)
        {
            float input = Input.GetAxis("Horizontal");

            // Управление в воздухе
            float targetSpeed = input * moveSpeed;

            float smoothX = Mathf.MoveTowards(
                playerbode.linearVelocity.x,
                targetSpeed,
                jetpackAirAcceleration * Time.deltaTime
            );

            playerbode.linearVelocity = new Vector2(
                smoothX,
                playerbode.linearVelocity.y
            );

            // Расход кислорода
            oxygen -= oxygenUseSpeed * Time.deltaTime;
        }
        else
        {
            // Восстановление кислорода
            if (onGround)
            {
                oxygen += oxygenRecoverSpeed * Time.deltaTime;
            }
        }
        // Ограничение кислорода
        oxygen = Mathf.Clamp(oxygen, 0, maxOxygen);

        // UI
        oxygenText.text = "H2: " + Mathf.Round(oxygen) + "%";
    }
    void UpdateAnimator()////Отвечает за различную анимацыю персонажа
    {
        ////Анимацыя бега
        if (Mathf.Abs(moveX) > 0.1f && onGround == true)
        {
            animator.SetFloat("Run", 1f);
        }
        else
        {
            animator.SetFloat("Run", 0f);
        }

        ////Анимацыя Прыжка
        if (onGround == false && atw == false)
        {
            animator.SetBool("Jump", true);
        }
        else
        {
            animator.SetBool("Jump", false);
        }

        ////Анимацыя Скальжения по стине
        if (atw && !onGround)
        {
            animator.SetBool("WallSlide", true);

            if (onGround == false && atw == true)
            {
                sr.flipX = true;
            }
            else
            {
                sr.flipX = false;
            }
        }
        else
        {
            animator.SetBool("WallSlide", false);
        }
    }
}
